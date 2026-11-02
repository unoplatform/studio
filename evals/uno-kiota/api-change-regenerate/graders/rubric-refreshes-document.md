---
type: llm
---
The reply gets a new model field and a new endpoint from an updated ASP.NET Core API into the generated Kiota client of an Uno Platform app.
Facts the judge needs: in the template's code-first setup, the client is generated from a copy of the server's OpenAPI document saved in `Specs/`, which `kiota-lock.json` points to, and the running server serves the current document at `/swagger/v1/swagger.json` in Development. Regenerating from the old copy changes nothing.
PASS if it refreshes the OpenAPI document the client is generated from before regenerating, by saving the running server's new `swagger.json` over the copy in `Specs/` or by generating from the server's URL.
FAIL if it regenerates from the old document unchanged.
