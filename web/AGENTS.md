# web/AGENTS.md: frontend session

You are working on the **CrowdAid frontend**. Read the root `../AGENTS.md` and `../CLAUDE.md` too.

- **Edit only files inside `web/`.** Do not create, edit, move or delete anything in `../api/`, `../design/`, or the root shared files. `web/Caddyfile` and `web/Dockerfile*` are shared hosting files: leave them alone unless asked.
- Design source: `../design/CrowdAid Prototype v2.dc.html` (open it in a browser to click through), `../design/CrowdAid Handoff v2.md` (tokens, components, states, validation), `../design/CrowdAid_LoFi_Wireframes_v0.4.1.excalidraw` (layout notes). Match it.
- API: `../api-spec-v3.md`. Until an endpoint exists, use mock data in the spec's exact JSON shape, one mock module per feature, behind a single switch so going live changes one line.
- Use relative `/api/...` URLs through one shared axios helper. Never `localhost`.
- No tests for now. Don't push, merge or deploy.
