$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    node -e "const fs=require('fs'),s=require('./tools/power-designer/shared-schema.cjs'),d=JSON.parse(fs.readFileSync('Assets/Resources/Content/prismatch.json'));s.validate(structuredClone(d),d);console.log('Shared catalog valid. No snapshot refresh is needed. Run Prismatch > Validate shared content in Unity for native-handler coverage.');"
    if ($LASTEXITCODE) { throw 'Shared content validation failed.' }
} finally { Pop-Location }
