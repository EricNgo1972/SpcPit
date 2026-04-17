// Download helper: trigger a browser download from a base64 payload.
window.spcPitDownload = function (fileName, base64, mimeType) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${mimeType || 'application/octet-stream'};base64,${base64}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
