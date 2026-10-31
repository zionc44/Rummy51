// This script sets up HTTPS for the application using the ASP.NET Core HTTPS certificate
const { createPrivateKey, X509Certificate } = require('crypto');
const fs = require('fs');
const { spawnSync } = require('child_process');
const path = require('path');

function isCertificateValid(certFilePath, keyFilePath, now = Date.now()) {
  try {
    const certificate = new X509Certificate(fs.readFileSync(certFilePath));
    const validFrom = Date.parse(certificate.validFrom);
    const validTo = Date.parse(certificate.validTo);

    if (!Number.isFinite(validFrom) || !Number.isFinite(validTo) || now < validFrom || now > validTo) {
      return false;
    }

    createPrivateKey(fs.readFileSync(keyFilePath));
    return true;
  } catch {
    return false;
  }
}

function main(runDotnet = spawnSync) {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ''
      ? `${process.env.APPDATA}/ASP.NET/https`
      : `${process.env.HOME}/.aspnet/https`;

  const certificateArg = process.argv.map(arg => arg.match(/--name=(?<value>.+)/i)).filter(Boolean)[0];
  const certificateName = certificateArg ? certificateArg.groups.value : process.env.npm_package_name;

  if (!certificateName) {
    console.error('Invalid certificate name. Run this script in the context of an npm/yarn script or pass --name=<<app>> explicitly.')
    return -1;
  }

  const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
  const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

  if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
  }

  if (!isCertificateValid(certFilePath, keyFilePath)) {
    fs.rmSync(certFilePath, { force: true });
    fs.rmSync(keyFilePath, { force: true });

    const result = runDotnet('dotnet', [
      'dev-certs',
      'https',
      '--export-path',
      certFilePath,
      '--format',
      'Pem',
      '--no-password',
    ], { stdio: 'inherit' });

    if (result.error) {
      console.error(`Failed to export the HTTPS developer certificate: ${result.error.message}`);
      return 1;
    }

    return result.status ?? 1;
  }

  return 0;
}

if (require.main === module) {
  process.exitCode = main();
}

module.exports = { isCertificateValid, main };