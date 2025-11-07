window.postAndDownload = async (url, payload, filename) => {
    const resp = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });
    if (!resp.ok) {
        const text = await resp.text().catch(() => "");
        throw new Error(`Download failed: ${resp.status} ${resp.statusText} ${text}`);
    }
    const blob = await resp.blob();
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 60_000);
};