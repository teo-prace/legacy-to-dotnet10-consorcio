# Spike 02a · Microsoft System.Web adapters (remote session)

The official alternative to the session bridge of [spike 02](../02-identity): the new app reads the
legacy session through Microsoft's System.Web adapters (`AddRemoteAppClient` + `AddSessionClient`).

It works. The cost is on the legacy side:

- 49 DLLs added to the legacy `Bin`;
- `web.config`: module, API key, `netstandard` reference, 8 binding redirects;
- `Global.asax`: host registration (static calls; VB extension methods did not resolve in a Web Site project);
- **the login rewritten** to store serializable keys, because the existing `DataRow` session value cannot
  be serialized (`ThrowOnUnknownSessionKey = false` on the new side).

[`preparar-legado.ps1`](preparar-legado.ps1) builds a modified copy of the legacy site in `%TEMP%`, so
the real legacy system is not touched. Measured cost: 1.3 ms per request (median).

Kept as **plan B**: if a new screen ever needs to *write* to the legacy session, this is proven.
Comparison and decision in [spike 02](../02-identity/README.md).
