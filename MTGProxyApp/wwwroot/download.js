window.triggerClick = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) el.click();
};

async function loadImageAsBase64(url) {
    const mode = url.startsWith('http') ? 'cors' : 'same-origin';
    const response = await fetch(url, { mode });
    const blob = await response.blob();
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

// pageGroups: string?[][][] - outer = groups (regular / flip-fronts / flip-backs),
//             middle = pages (chunks of 9), inner = card URL or null for empty slot
// options: { blackCorners, borders, fileName }
window.generateProxyPdf = async (pageGroups, options) => {
    const { jsPDF } = window.jspdf;

    const cardW  = 63;    // mm — standard MTG card
    const cardH  = 88;
    const pageW  = 215.9; // mm — US Letter
    const pageH  = 279.4;
    const cols   = 3;
    const rows   = 3;
    const marginX = (pageW - cols * cardW) / 2;
    const marginY = (pageH - rows * cardH) / 2;

    // Collect unique URLs for parallel pre-loading
    const allUrls = new Set();
    for (const group of pageGroups)
        for (const page of group)
            for (const url of page)
                if (url) allUrls.add(url);

    const cache = {};
    await Promise.all([...allUrls].map(async url => {
        try { cache[url] = await loadImageAsBase64(url); }
        catch (e) { console.warn('Could not load card image:', url, e); }
    }));

    const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'letter' });
    let firstPage = true;

    for (const group of pageGroups) {
        for (const page of group) {
            if (!firstPage) pdf.addPage('letter', 'portrait');
            firstPage = false;

            // Light cut-guide lines at card boundaries
            pdf.setDrawColor(180, 180, 180);
            pdf.setLineWidth(0.1);
            for (let c = 0; c <= cols; c++) {
                const x = marginX + c * cardW;
                pdf.line(x, 0, x, pageH);
            }
            for (let r = 0; r <= rows; r++) {
                const y = marginY + r * cardH;
                pdf.line(0, y, pageW, y);
            }

            for (let i = 0; i < page.length; i++) {
                const url = page[i];
                if (!url || !cache[url]) continue;

                const col = i % cols;
                const row = Math.floor(i / cols);
                const x = marginX + col * cardW;
                const y = marginY + row * cardH;

                if (options.blackCorners) {
                    pdf.setFillColor(0, 0, 0);
                    pdf.rect(x, y, cardW, cardH, 'F');
                }

                const imgData = cache[url];
                const fmt = imgData.startsWith('data:image/png') ? 'PNG' : 'JPEG';
                // Use url as alias so duplicate cards reuse the embedded image data
                pdf.addImage(imgData, fmt, x, y, cardW, cardH, url, 'FAST');

                if (options.borders) {
                    pdf.setDrawColor(0, 0, 0);
                    pdf.setLineWidth(0.2);
                    pdf.rect(x, y, cardW, cardH, 'S');
                }

                if (options.blackCorners) {
                    const cl = 3; // corner cross arm length in mm
                    pdf.setDrawColor(255, 255, 255);
                    pdf.setLineWidth(0.3);
                    // top-left
                    pdf.line(x,            y + cl / 2,  x + cl,        y + cl / 2);
                    pdf.line(x + cl / 2,   y,           x + cl / 2,    y + cl);
                    // top-right
                    pdf.line(x + cardW - cl, y + cl / 2, x + cardW,     y + cl / 2);
                    pdf.line(x + cardW - cl / 2, y,      x + cardW - cl / 2, y + cl);
                    // bottom-left
                    pdf.line(x,            y + cardH - cl / 2, x + cl,    y + cardH - cl / 2);
                    pdf.line(x + cl / 2,   y + cardH - cl,     x + cl / 2, y + cardH);
                    // bottom-right
                    pdf.line(x + cardW - cl, y + cardH - cl / 2, x + cardW,          y + cardH - cl / 2);
                    pdf.line(x + cardW - cl / 2, y + cardH - cl, x + cardW - cl / 2, y + cardH);
                }
            }
        }
    }

    pdf.save(`${options.fileName || 'deck'}.pdf`);
};
