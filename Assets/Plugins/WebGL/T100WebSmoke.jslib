mergeInto(LibraryManager.library, {
  T100WebSmokeReport: function (keyPointer, valuePointer) {
    var key = UTF8ToString(keyPointer);
    var value = UTF8ToString(valuePointer);
    var state = window.__oneStrokeWebSmoke || { events: [] };
    state[key] = value;
    state.events.push({ key: key, value: value, at: Date.now() });
    window.__oneStrokeWebSmoke = state;

    var panel = document.getElementById("one-stroke-web-smoke");
    if (!panel) {
      panel = document.createElement("div");
      panel.id = "one-stroke-web-smoke";
      panel.style.cssText = [
        "position:fixed",
        "top:8px",
        "right:8px",
        "z-index:9999",
        "max-width:44vw",
        "padding:8px 10px",
        "border-radius:6px",
        "background:rgba(0,0,0,.78)",
        "color:#fff",
        "font:12px/1.45 -apple-system,BlinkMacSystemFont,sans-serif",
        "white-space:pre-wrap",
        "pointer-events:none"
      ].join(";");
      document.body.appendChild(panel);
    }

    panel.dataset.runtime = state.runtime || "";
    panel.dataset.input = state.input || "";
    panel.dataset.audio = state.audio || "";
    panel.dataset.storage = state.storage || "";
    panel.dataset.storageRun = state.storageRun || "";
    panel.dataset.scene = state.scene || "";
    panel.textContent = [
      "T100 Web Smoke",
      "runtime=" + (state.runtime || "waiting"),
      "scene=" + (state.scene || "waiting"),
      "input=" + (state.input || "waiting"),
      "audio=" + (state.audio || "waiting"),
      "storage=" + (state.storage || "waiting") + " run=" + (state.storageRun || "?"),
      "chinese=" + (state.chinese || "waiting")
    ].join("\n");

    window.dispatchEvent(new CustomEvent("one-stroke-web-smoke", {
      detail: { key: key, value: value }
    }));
  }
});
