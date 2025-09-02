# Already installed mkcert via chocolatey.
# Already ran mkcert -install

# Create local wildcard cert
 mkcert "*.platform.local"
 
# Convert to PFX format
openssl pkcs12 -export -out _wildcard.platform.local.pfx -inkey _wildcard.platform.local-key.pem -in _wildcard.platform.local.pem -certfile "$LOCALAPPDATA/mkcert/rootCA.pem"

