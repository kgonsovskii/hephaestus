# Install IIS URL Rewrite Module
function Install-UrlRewrite {
    choco install urlrewrite --yes --ignore-checksums --no-progress
}
Install-UrlRewrite