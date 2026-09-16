using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Nvidea.DemoPackageValidator.Tests;

public sealed class DemoPackageValidatorTests
{
    private static readonly string[] RequiredAssets =
    [
        "README.md", "LICENSE", "docs/personal-ai-demo-eval.md", "docs/personal-ai-adversarial-eval.md",
        "docs/judging-evidence-verifier.md", "tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj",
        "tools/Nvidea.PersonalAiAdversarialEval/Nvidea.PersonalAiAdversarialEval.csproj",
        "tools/Nvidea.JudgingEvidenceVerifier/Nvidea.JudgingEvidenceVerifier.csproj"
    ];

    private static readonly string[] BeatIds =
    ["invoke-context", "durable-memory", "tavily-research", "browser-verify", "permission-gate", "nebius-background", "architecture-proof"];

    private static readonly string[][] Milestones =
    [
        ["NemotronInferenceCompleted"], ["MemoryInfluencedResponse"], ["TavilyValidatedCitationUsed"],
        ["BrowserVerifiedGoalCompleted"], ["ConsequentialApprovalGranted"], ["NebiusBackgroundExecutionObserved"], []
    ];

    [Fact] public async Task Valid_schema_v2_manifest_passes() { using var f = new ValidatorFixture(); Assert.Equal(0, await f.RunAsync(f.CreateManifest())); }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task Missing_preflight_fails_closed(int beat) { using var f = new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,beat)["preflightDependencies"]=Array.Empty<string>(); Assert.Equal(1,await f.RunAsync(m)); }

    [Theory]
    [InlineData(0)] [InlineData(5)] [InlineData(6)]
    public async Task Blank_fallback_fails_closed(int beat) { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,beat)["fallbackPolicy"]=" "; Assert.Equal(1,await f.RunAsync(m)); }

    [Fact] public async Task Wrong_session_milestone_is_rejected() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,2)["expectedSessionMilestones"]=new[]{"NemotronInferenceCompleted"}; Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Missing_session_milestone_is_rejected() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,4)["expectedSessionMilestones"]=Array.Empty<string>(); Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Architecture_beat_cannot_claim_runtime_milestone() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,6)["expectedSessionMilestones"]=new[]{"NemotronInferenceCompleted"}; Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Duration_overflow_is_rejected() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,0)["durationSeconds"]=181; Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Missing_required_asset_is_rejected() { using var f=new ValidatorFixture(); File.Delete(Path.Combine(f.Root,"LICENSE")); Assert.Equal(1,await f.RunAsync(f.CreateManifest())); }

    [Theory]
    [InlineData("fallbackPolicy", "authorization: bearer top-secret")]
    [InlineData("fallbackPolicy", "api_key=top-secret")]
    public async Task Secret_in_v2_fallback_is_rejected(string field,string value) { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,0)[field]=value; Assert.Equal(1,await f.RunAsync(m)); }

    [Fact] public async Task Secret_in_v2_preflight_is_rejected() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,1)["preflightDependencies"]=new[]{"apikey=top-secret"}; Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Secret_in_v2_milestone_is_rejected_even_before_contract_match() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,3)["expectedSessionMilestones"]=new[]{"sk-top-secret"}; Assert.Equal(1,await f.RunAsync(m)); }

    [Fact]
    public async Task Duplicate_json_property_is_rejected()
    {
        using var f=new ValidatorFixture();
        var json=JsonSerializer.Serialize(f.CreateManifest());
        json=json.Replace("\"schemaVersion\":2", "\"schemaVersion\":2,\"schemaVersion\":2", StringComparison.Ordinal);
        Assert.Equal(1,await f.RunRawAsync(json));
    }

    [Fact] public async Task Path_traversal_is_rejected() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,0)["featurePaths"]=new[]{"../outside.txt"}; Assert.Equal(1,await f.RunAsync(m)); }
    [Fact] public async Task Provider_live_claim_requires_provider_live_evidence() { using var f=new ValidatorFixture(); var m=f.CreateManifest(); f.Beat(m,5)["requiresProviderLive"]=true; Assert.Equal(1,await f.RunAsync(m)); }

    private sealed class ValidatorFixture : IDisposable
    {
        public ValidatorFixture()
        {
            Root=Path.Combine(Path.GetTempPath(),"nvidea-demo-validator-tests",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Root);
            foreach(var a in RequiredAssets) Write(a);
            foreach(var p in new[]{"src/Nvidea.Core/Desktop","src/Nvidea.Core/Memory","src/Nvidea.Core/Research","src/Nvidea.Core/Browser","src/Nvidea.Core/Capabilities"}) Directory.CreateDirectory(Path.Combine(Root,p));
            Write("docs/evidence.json");
        }
        public string Root { get; }
        public Dictionary<string,object?> Beat(Dictionary<string,object?> m,int i)=>((List<Dictionary<string,object?>>)m["beats"]!)[i];
        public Dictionary<string,object?> CreateManifest()
        {
            var paths=new[]{"src/Nvidea.Core/Desktop","src/Nvidea.Core/Memory","src/Nvidea.Core/Research","src/Nvidea.Core/Browser","src/Nvidea.Core/Capabilities","src/Nvidea.Core/Research","README.md"};
            var beats=new List<Dictionary<string,object?>>();
            for(var i=0;i<BeatIds.Length;i++) beats.Add(new()
            {
                ["id"]=BeatIds[i],["label"]=BeatIds[i],["durationSeconds"]=20,["judgeClaim"]="Deterministic demo claim",["requiresProviderLive"]=false,
                ["expectedSessionMilestones"]=Milestones[i],["preflightDependencies"]=new[]{"Required local dependency is ready"},["fallbackPolicy"]="Fail closed; do not claim live evidence.",
                ["featurePaths"]=new[]{paths[i]},["evidence"]=new[]{new Dictionary<string,object?>{{"evidenceClass","synthetic"},{"path","docs/evidence.json"},{"claim","Synthetic validation only"}}}
            });
            return new(){{"schemaVersion",2},{"title","NVIDEA deterministic demo"},{"maxDurationSeconds",180},{"beats",beats},{"commands",new List<Dictionary<string,object?>>{new(){{"label","Positive evaluator"},{"projectPath","tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj"},{"command","dotnet run --project tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj"}}}};
        }
        public Task<int> RunAsync(Dictionary<string,object?> m)=>RunRawAsync(JsonSerializer.Serialize(m));
        public async Task<int> RunRawAsync(string json)
        {
            var path=Path.Combine(Root,"demo.json"); await File.WriteAllTextAsync(path,json);
            var program=Assembly.Load("Nvidea.DemoPackageValidator").GetType("Nvidea.DemoPackageValidator.Program",true)!;
            var main=program.GetMethod("Main",BindingFlags.Public|BindingFlags.Static)??throw new InvalidOperationException("Validator Main missing.");
            return await (Task<int>)main.Invoke(null,new object[]{new[]{Root,path}})!;
        }
        private void Write(string p){var f=Path.Combine(Root,p.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(f)!);File.WriteAllText(f,"fixture");}
        public void Dispose(){if(Directory.Exists(Root))Directory.Delete(Root,true);}
    }
}
