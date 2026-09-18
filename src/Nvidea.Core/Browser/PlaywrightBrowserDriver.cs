namespace Nvidea.Core.Browser;

using System.Text.RegularExpressions;
using Microsoft.Playwright;

public sealed record PlaywrightBrowserDriverOptions(
    IReadOnlySet<string>? AllowedHosts = null,
    int MaxObservationCharacters = 12_000,
    int MaxObservedElements = 250,
    float ActionTimeoutMilliseconds = 15_000)
{
    public IReadOnlySet<string> NormalizedAllowedHosts { get; } =
        new HashSet<string>(AllowedHosts ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
}

public sealed class PlaywrightBrowserDriver : IBrowserDriver
{
    private const string RefAttribute = "data-nvidea-ref";
    private static readonly Regex PromptInjectionPattern = new(
        @"(?ix)\b(ignore|disregard|override)\b.{0,40}\b(previous|prior|system|developer|instructions?)\b|\b(system|developer)\s+(message|instruction)\b|\breveal\b.{0,32}\b(secret|credential|api\s*key|password)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private readonly IPage _page;
    private readonly PlaywrightBrowserDriverOptions _options;

    public PlaywrightBrowserDriver(IPage page, PlaywrightBrowserDriverOptions? options = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _options = options ?? new PlaywrightBrowserDriverOptions();
        if (_options.MaxObservationCharacters is < 512 or > 100_000) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.MaxObservedElements is < 1 or > 2_000) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.ActionTimeoutMilliseconds is < 250 or > 120_000) throw new ArgumentOutOfRangeException(nameof(options));
    }

    public async Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var url = ParseHttpUri(_page.Url, true);
        var title = await _page.TitleAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        var snapshot = await _page.EvaluateAsync<DomObservationSnapshot>(
            """
            ({ maxElements, maxText, sensitiveAutocompleteTokens }) => {
              const refAttr = 'data-nvidea-ref';
              const sensitiveTokens = new Set((sensitiveAutocompleteTokens || []).map(x => String(x).trim().toLowerCase()).filter(Boolean));
              const candidates = Array.from(document.querySelectorAll('a,button,input,textarea,select,[role],[contenteditable="true"],[tabindex]')).slice(0, maxElements);
              const inferRole = (el) => {
                const explicit = el.getAttribute('role'); if (explicit) return explicit.toLowerCase();
                const tag = el.tagName.toLowerCase();
                if (tag === 'a') return 'link'; if (tag === 'button') return 'button'; if (tag === 'textarea') return 'textbox'; if (tag === 'select') return 'combobox';
                if (tag === 'input') { const type=(el.getAttribute('type')||'text').toLowerCase(); if(type==='checkbox')return 'checkbox'; if(type==='radio')return 'radio'; if(['button','submit','reset'].includes(type))return 'button'; return 'textbox'; }
                return tag;
              };
              const accessibleName = (el) => {
                const aria=el.getAttribute('aria-label'); if(aria)return aria.trim();
                const labelledBy=el.getAttribute('aria-labelledby'); if(labelledBy){const text=labelledBy.split(/\s+/).map(id=>document.getElementById(id)?.innerText||'').join(' ').trim();if(text)return text;}
                if(el.labels&&el.labels.length){const text=Array.from(el.labels).map(x=>x.innerText||'').join(' ').trim();if(text)return text;}
                return (el.innerText||el.getAttribute('placeholder')||el.getAttribute('title')||el.getAttribute('alt')||'').trim();
              };
              const visible=(el)=>{const style=getComputedStyle(el);const rect=el.getBoundingClientRect();return style.visibility!=='hidden'&&style.display!=='none'&&rect.width>0&&rect.height>0;};
              const elements=candidates.map((el,i)=>{
                const ref=`nv-${i+1}`; el.setAttribute(refAttr,ref);
                const tag=el.tagName.toLowerCase();
                // Only expose bounded, non-secret form semantics. Decide suppression entirely from
                // metadata before touching el.value so password/OTP/payment values never enter the snapshot.
                const inputType=tag==='input' ? (el.getAttribute('type')||'text').trim().toLowerCase().slice(0,64) : null;
                const autoComplete=(tag==='input'||tag==='textarea'||tag==='select') ? (el.getAttribute('autocomplete')||'').trim().toLowerCase().replace(/\s+/g,' ').slice(0,128) : null;
                const suppressValue=inputType==='password'||(autoComplete&&autoComplete.split(/\s+/).some(token=>sensitiveTokens.has(token)));
                const value=suppressValue?null:('value' in el?String(el.value??''):null);
                return { reference:ref, role:inferRole(el), name:accessibleName(el)||null, value,
                  isVisible:visible(el), isEnabled:!el.disabled&&el.getAttribute('aria-disabled')!=='true',
                  isEditable:tag==='textarea'||tag==='select'||el.isContentEditable||(tag==='input'&&!['button','submit','reset','checkbox','radio','file'].includes(inputType)),
                  isChecked:Boolean(el.checked), inputType, autoComplete:autoComplete||null };
              });
              const text=(document.body?.innerText||'').replace(/\u0000/g,'').slice(0,maxText);
              return {visibleText:text,elements};
            }
            """, new
            {
                maxElements = _options.MaxObservedElements,
                maxText = _options.MaxObservationCharacters,
                sensitiveAutocompleteTokens = BrowserObservedValuePrivacyPolicy.SensitiveAutocompleteTokens
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var visibleText=snapshot?.VisibleText??string.Empty;
        var elements=(snapshot?.Elements??Array.Empty<DomElementSnapshot>()).Take(_options.MaxObservedElements)
            .Select(e=>new BrowserElement(e.Reference??string.Empty,e.Role??"unknown",e.Name,e.Value,e.IsVisible,e.IsEnabled,e.IsEditable,e.IsChecked,e.InputType,e.AutoComplete))
            .Where(e=>!string.IsNullOrWhiteSpace(e.Reference)).ToArray();
        return new BrowserObservation(url,title,elements,visibleText,DateTimeOffset.UtcNow,PromptInjectionPattern.IsMatch(visibleText),Guid.NewGuid().ToString("N"));
    }

    public async Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action); cancellationToken.ThrowIfCancellationRequested(); var timeout=_options.ActionTimeoutMilliseconds;
        switch(action.Kind){case BrowserActionKind.Read:return;case BrowserActionKind.Navigate:{var d=action.Destination??throw new InvalidOperationException("Navigate requires a destination URI.");ValidateDestination(d);await AwaitActionOrAbortPageAsync(_page.GotoAsync(d.AbsoluteUri,new PageGotoOptions{Timeout=timeout,WaitUntil=WaitUntilState.DOMContentLoaded}),cancellationToken).ConfigureAwait(false);return;}case BrowserActionKind.Back:await AwaitActionOrAbortPageAsync(_page.GoBackAsync(new PageGoBackOptions{Timeout=timeout,WaitUntil=WaitUntilState.DOMContentLoaded}),cancellationToken).ConfigureAwait(false);return;case BrowserActionKind.Refresh:await AwaitActionOrAbortPageAsync(_page.ReloadAsync(new PageReloadOptions{Timeout=timeout,WaitUntil=WaitUntilState.DOMContentLoaded}),cancellationToken).ConfigureAwait(false);return;}
        var locator=ResolveLocator(action.Locator??throw new InvalidOperationException($"{action.Kind} requires a locator."));
        switch(action.Kind){case BrowserActionKind.Click:await AwaitActionOrAbortPageAsync(locator.ClickAsync(new LocatorClickOptions{Timeout=timeout}),cancellationToken).ConfigureAwait(false);break;case BrowserActionKind.Type:await AwaitActionOrAbortPageAsync(locator.FillAsync(action.Value??string.Empty,new LocatorFillOptions{Timeout=timeout}),cancellationToken).ConfigureAwait(false);break;case BrowserActionKind.Select:await AwaitActionOrAbortPageAsync(locator.SelectOptionAsync(action.Value??throw new InvalidOperationException("Select requires a value."),new LocatorSelectOptionOptions{Timeout=timeout}),cancellationToken).ConfigureAwait(false);break;case BrowserActionKind.Upload:await AwaitActionOrAbortPageAsync(locator.SetInputFilesAsync(action.Value??throw new InvalidOperationException("Upload requires a local file path."),new LocatorSetInputFilesOptions{Timeout=timeout}),cancellationToken).ConfigureAwait(false);break;case BrowserActionKind.Download:await AwaitActionOrAbortPageAsync(locator.ClickAsync(new LocatorClickOptions{Timeout=timeout}),cancellationToken).ConfigureAwait(false);break;default:throw new NotSupportedException($"Unsupported browser action: {action.Kind}.");}
    }

    private async Task AwaitActionOrAbortPageAsync(Task operation,CancellationToken cancellationToken){if(!cancellationToken.CanBeCanceled){await operation.ConfigureAwait(false);return;}var signal=Task.Delay(Timeout.InfiniteTimeSpan,cancellationToken);var completed=await Task.WhenAny(operation,signal).ConfigureAwait(false);if(completed==operation){await operation.ConfigureAwait(false);return;}try{await _page.CloseAsync(new PageCloseOptions{RunBeforeUnload=false}).ConfigureAwait(false);}catch(PlaywrightException){}if(!operation.IsCompleted)_=operation.ContinueWith(static task=>_=task.Exception,CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);cancellationToken.ThrowIfCancellationRequested();throw new OperationCanceledException(cancellationToken);}
    private ILocator ResolveLocator(BrowserLocator locator){var value=RequireLocatorValue(locator.Value);return locator.Kind switch{BrowserLocatorKind.AccessibilityRef=>_page.Locator($"[{RefAttribute}=\"{EscapeCssAttribute(value)}\"]"),BrowserLocatorKind.RoleAndName=>_page.GetByRole(ParseAriaRole(locator.Role),new PageGetByRoleOptions{Name=locator.Name??value,Exact=true}),BrowserLocatorKind.Label=>_page.GetByLabel(value,new PageGetByLabelOptions{Exact=true}),BrowserLocatorKind.Text=>_page.GetByText(value,new PageGetByTextOptions{Exact=true}),BrowserLocatorKind.TestId=>_page.GetByTestId(value),BrowserLocatorKind.Css=>ResolveCssFallback(value),_=>throw new NotSupportedException($"Unsupported locator kind: {locator.Kind}.")};}
    private ILocator ResolveCssFallback(string selector){if(selector.Length>512||selector.Contains("xpath=",StringComparison.OrdinalIgnoreCase)||selector.StartsWith("//",StringComparison.Ordinal))throw new InvalidOperationException("Only bounded CSS fallback selectors are permitted; XPath is intentionally disabled.");return _page.Locator(selector);}
    private Uri ParseHttpUri(string raw,bool enforceHostBoundary){if(!Uri.TryCreate(raw,UriKind.Absolute,out var uri))throw new InvalidOperationException("Browser page does not have a valid absolute URL.");if(uri.Scheme is not("http" or "https"))throw new InvalidOperationException($"Browser scheme '{uri.Scheme}' is outside the allowed web boundary.");if(enforceHostBoundary)EnsureAllowedHost(uri);return uri;}
    private void ValidateDestination(Uri destination){if(!destination.IsAbsoluteUri||destination.Scheme is not("http" or "https"))throw new InvalidOperationException("Navigation is restricted to absolute HTTP(S) URLs.");EnsureAllowedHost(destination);}
    private void EnsureAllowedHost(Uri uri){var allowed=_options.NormalizedAllowedHosts;if(allowed.Count!=0&&!allowed.Contains(uri.IdnHost))throw new InvalidOperationException($"Host '{uri.IdnHost}' is outside this browser context's allowlist.");}
    private static string RequireLocatorValue(string value)=>!string.IsNullOrWhiteSpace(value)?value.Trim():throw new InvalidOperationException("Locator value is required.");
    private static string EscapeCssAttribute(string value)=>value.Replace("\\","\\\\",StringComparison.Ordinal).Replace("\"","\\\"",StringComparison.Ordinal);
    private static AriaRole ParseAriaRole(string? role)=>(role??string.Empty).Trim().ToLowerInvariant() switch{"alert"=>AriaRole.Alert,"button"=>AriaRole.Button,"checkbox"=>AriaRole.Checkbox,"combobox"=>AriaRole.Combobox,"dialog"=>AriaRole.Dialog,"heading"=>AriaRole.Heading,"link"=>AriaRole.Link,"listbox"=>AriaRole.Listbox,"menuitem"=>AriaRole.Menuitem,"option"=>AriaRole.Option,"radio"=>AriaRole.Radio,"searchbox"=>AriaRole.Searchbox,"spinbutton"=>AriaRole.Spinbutton,"switch"=>AriaRole.Switch,"tab"=>AriaRole.Tab,"textbox"=>AriaRole.Textbox,_=>throw new InvalidOperationException($"Unsupported accessibility role '{role}'. Use another user-facing locator or bounded CSS fallback.")};
    private sealed class DomObservationSnapshot{public string? VisibleText{get;set;}public DomElementSnapshot[]? Elements{get;set;}}
    private sealed class DomElementSnapshot{public string? Reference{get;set;}public string? Role{get;set;}public string? Name{get;set;}public string? Value{get;set;}public bool IsVisible{get;set;}public bool IsEnabled{get;set;}public bool IsEditable{get;set;}public bool IsChecked{get;set;}public string? InputType{get;set;}public string? AutoComplete{get;set;}}
}
