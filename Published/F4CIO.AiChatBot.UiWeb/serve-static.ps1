# Minimal static file server with SPA fallback (serves a Vite 'dist' folder).
# Works on stock Windows (incl. Server 2012 R2) - PowerShell only, no Node, no installs.
param(
    [int]$Port = 5102,
    [string]$Root = $PSScriptRoot
)

$listener = New-Object System.Net.HttpListener
try {
    # Prefer all-interfaces (LAN). Needs admin or a urlacl; fall back to localhost.
    $listener.Prefixes.Add("http://+:$Port/")
    $listener.Start()
} catch {
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://localhost:$Port/")
    $listener.Start()
    Write-Host "(Localhost only - run as Administrator for LAN access.)"
}

Write-Host "Serving '$Root' at http://localhost:$Port/  (Ctrl+C to stop)"

$mime = @{
    ".html" = "text/html; charset=utf-8"; ".js" = "text/javascript"; ".mjs" = "text/javascript";
    ".css" = "text/css"; ".json" = "application/json"; ".svg" = "image/svg+xml";
    ".png" = "image/png"; ".jpg" = "image/jpeg"; ".jpeg" = "image/jpeg"; ".gif" = "image/gif";
    ".ico" = "image/x-icon"; ".woff" = "font/woff"; ".woff2" = "font/woff2";
    ".map" = "application/json"; ".webmanifest" = "application/manifest+json"; ".txt" = "text/plain"
}

try {
    while ($listener.IsListening) {
        $ctx = $listener.GetContext()
        try {
            $rel = [System.Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath.TrimStart('/'))
            if ([string]::IsNullOrWhiteSpace($rel)) { $rel = "index.html" }
            $path = Join-Path $Root $rel
            # SPA fallback: anything that isn't a real file serves index.html.
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $path = Join-Path $Root "index.html" }

            $bytes = [System.IO.File]::ReadAllBytes($path)
            $ext = [System.IO.Path]::GetExtension($path).ToLowerInvariant()
            $ctx.Response.ContentType = if ($mime.ContainsKey($ext)) { $mime[$ext] } else { "application/octet-stream" }
            $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } catch {
            $ctx.Response.StatusCode = 500
        } finally {
            $ctx.Response.OutputStream.Close()
        }
    }
} finally {
    $listener.Stop()
}
