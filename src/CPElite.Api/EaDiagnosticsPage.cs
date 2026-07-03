namespace CPElite.Api;

internal static class EaDiagnosticsPage
{
    public const string Html = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>CPElite EA Diagnostics</title>
  <style>
    :root { color-scheme: light; font-family: Inter, Segoe UI, Arial, sans-serif; color: #172026; background: #f5f7f9; }
    body { margin: 0; padding: 32px; }
    main { max-width: 980px; margin: 0 auto; }
    h1 { margin: 0 0 6px; font-size: 28px; }
    p { color: #53616d; line-height: 1.5; }
    section { background: #fff; border: 1px solid #dfe5ea; border-radius: 8px; padding: 20px; margin-top: 18px; }
    label { display: block; font-size: 13px; font-weight: 700; margin-bottom: 6px; color: #33424d; }
    input, select { width: 100%; box-sizing: border-box; border: 1px solid #cbd5dc; border-radius: 6px; padding: 10px 12px; font: inherit; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    button { border: 0; border-radius: 6px; background: #175cd3; color: white; padding: 11px 14px; font-weight: 700; cursor: pointer; }
    button.secondary { background: #344054; }
    button:disabled { opacity: .55; cursor: not-allowed; }
    pre { overflow: auto; background: #101828; color: #d1fadf; padding: 16px; border-radius: 8px; min-height: 180px; white-space: pre-wrap; }
    .row { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; margin-top: 14px; }
    .status { font-weight: 700; color: #175cd3; }
    @media (max-width: 720px) { body { padding: 18px; } .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <h1>CPElite EA Diagnostics</h1>
    <p>Use this local page to test whether this machine can reach EA Pro Clubs endpoints and inspect the response preview.</p>

    <section>
      <h2>1. Local Test Login</h2>
      <p>This creates a temporary local user in the in-memory database and stores the token in this page.</p>
      <div class="row">
        <button id="loginButton">Create local test user</button>
        <span id="authStatus" class="status">Not connected</span>
      </div>
    </section>

    <section>
      <h2>2. EA Probe</h2>
      <div class="grid">
        <div>
          <label for="platform">Platform</label>
          <select id="platform">
            <option value="common-gen5">common-gen5</option>
            <option value="pc">pc</option>
          </select>
        </div>
        <div>
          <label for="matchType">Match type</label>
          <select id="matchType">
            <option value="friendlyMatch">friendlyMatch</option>
            <option value="leagueMatch">leagueMatch</option>
            <option value="playoffMatch">playoffMatch</option>
          </select>
        </div>
        <div>
          <label for="clubName">Club name</label>
          <input id="clubName" value="TheSurvivors">
        </div>
        <div>
          <label for="clubId">EA club ID, optional</label>
          <input id="clubId" placeholder="Paste club ID if known">
        </div>
      </div>
      <div class="row">
        <button id="probeButton" disabled>Run EA probe</button>
        <button id="copyButton" class="secondary">Copy result</button>
      </div>
    </section>

    <section>
      <h2>Result</h2>
      <pre id="output">Create a local test user, then run the EA probe.</pre>
    </section>
  </main>

  <script>
    let token = "";
    const output = document.getElementById("output");
    const authStatus = document.getElementById("authStatus");
    const probeButton = document.getElementById("probeButton");

    function print(value) {
      output.textContent = typeof value === "string" ? value : JSON.stringify(value, null, 2);
    }

    function printProbe(value) {
      if (!value || !Array.isArray(value.steps)) {
        print(value);
        return;
      }

      const lines = [
        `EA probe at ${value.testedAt}`,
        `Platform: ${value.platform}`,
        `Club: ${value.clubName || "-"} / ID: ${value.clubId || "-"}`,
        "",
        ...value.steps.flatMap(step => [
          `${step.success ? "OK" : "FAILED"} ${step.name}`,
          `  Endpoint: ${step.endpoint}`,
          `  Status: ${step.statusCode || "-"} ${step.error || ""}`,
          `  Raw length: ${step.rawLength || 0}`,
          `  Preview: ${step.rawPreview || "-"}`,
          ""
        ])
      ];

      output.textContent = lines.join("\n");
    }

    document.getElementById("loginButton").addEventListener("click", async () => {
      const email = `ea-probe-${crypto.randomUUID()}@local.test`;
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email,
          password: "Password123",
          displayName: "EA Probe",
          gamertag: "EAProbe",
          eaSportsId: null,
          platform: 4,
          preferredLanguage: "en",
          timeZone: "Europe/Zurich"
        })
      });

      const body = await response.json();
      if (!response.ok) {
        print(body);
        return;
      }

      token = body.accessToken;
      authStatus.textContent = `Connected as ${body.user.email}`;
      probeButton.disabled = false;
      print("Connected. You can run the EA probe now.");
    });

    probeButton.addEventListener("click", async () => {
      const clubIdRaw = document.getElementById("clubId").value.trim();
      const response = await fetch("/api/ea/diagnostics/probe", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({
          platform: document.getElementById("platform").value,
          clubName: document.getElementById("clubName").value.trim() || null,
          clubId: clubIdRaw ? Number(clubIdRaw) : null,
          matchType: document.getElementById("matchType").value,
          maxResults: 10
        })
      });

      const text = await response.text();
      try { printProbe(JSON.parse(text)); } catch { print(text); }
    });

    document.getElementById("copyButton").addEventListener("click", async () => {
      await navigator.clipboard.writeText(output.textContent);
    });
  </script>
</body>
</html>
""";
}
