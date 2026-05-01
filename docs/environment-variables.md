# Environment Variable Parameters

Parameters can read their values from environment variables at load time. This lets you keep
sensitive information — connection strings, API keys, passwords — out of your arrangements and
common settings entirely.

## Configuration

Add an `env` attribute to any `<add>` element inside `<parameters>`, pointing to the environment
variable whose value should be used:

```xml
<parameters>
  <add name="ConnectionString" env="MY_DB_CONNECTION_STRING" />
  <add name="ApiKey" env="MY_API_KEY" />
</parameters>
```

When the arrangement is loaded, if no external caller has already supplied a value for the
parameter, the module reads the named environment variable and uses it as the parameter's value.

## How It Works

- If a caller (e.g. a form submission or a query-string value) already provides a value for
  the parameter, the environment variable is **not** consulted — caller-supplied values take
  precedence.
- If the parameter has no value and `env` is set, `Environment.GetEnvironmentVariable(env)` is
  called and the result becomes the parameter's value for that request.
- If the environment variable is not set (returns `null`), the parameter remains empty.

## Where It Applies

Environment variable resolution runs in both contexts:

- **Common settings** — the shared arrangement loaded from the module's site settings.
- **Content item arrangements** — per-content-item XML/JSON arrangements.

## Typical Use Case

Store a database password in an environment variable on the server and reference it in your
arrangement without ever writing the literal value into Orchard content or settings:

```xml
<connections>
  <add name="input"
       provider="sqlserver"
       server="myserver"
       database="mydb"
       user="myuser"
       password="@[DbPassword]" />
</connections>
<parameters>
  <add name="DbPassword" env="DB_PASSWORD" />
</parameters>
```

Setting `input="false"` ensures the parameter is never exposed to end-user input — it is
resolved from the environment and injected into the arrangement internally.