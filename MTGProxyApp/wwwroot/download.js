window.downloadFileFromBytes = (filename, base64) => {
    const link = document.createElement('a');
    link.href = "data:application/zip;base64," + base64;
    link.download = filename;
    link.click();
};