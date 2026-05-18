window.triggerClick = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) el.click();
};

// Renders the card-sheet HTML in a hidden iframe and triggers the browser
// print dialog so the user can save to PDF or send to a printer.
window.openPrintWindow = (htmlContent) => {
    const existing = document.getElementById('_printFrame');
    if (existing) existing.remove();

    const iframe = document.createElement('iframe');
    iframe.id = '_printFrame';
    iframe.style.cssText = 'position:fixed;width:0;height:0;border:none;';
    document.body.appendChild(iframe);

    const doc = iframe.contentDocument || iframe.contentWindow.document;
    doc.open();
    doc.write(htmlContent);
    doc.close();

    // The inline <script> inside htmlContent handles waiting for images
    // and then calls window.print() on its own window (the iframe).
    // Clean up after the user finishes (or cancels) the print dialog.
    iframe.contentWindow.addEventListener('afterprint', () => iframe.remove());
};
