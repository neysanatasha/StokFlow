// Pembantu unduh file: menerima isi file dari C# lalu memicu unduhan di browser
window.downloadFile = function (fileName, base64, contentType) {
    const link = document.createElement('a');
    link.href = 'data:' + contentType + ';base64,' + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
};