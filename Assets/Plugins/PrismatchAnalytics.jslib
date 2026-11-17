mergeInto(LibraryManager.library, {
  PrismatchAnalyticsDownload: function(filename, text) {
    var blob = new Blob([UTF8ToString(text)], {type: /\.json$/i.test(UTF8ToString(filename)) ? 'application/json;charset=utf-8' : 'text/html;charset=utf-8'});
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url;
    link.download = UTF8ToString(filename);
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(function() { URL.revokeObjectURL(url); }, 60000);
  },
  PrismatchAnalyticsSync: function() {
    if (typeof FS === 'undefined' || !FS.syncfs) return;
    var state = Module.prismatchAnalyticsSync || (Module.prismatchAnalyticsSync = {busy: false, dirty: false});
    state.dirty = true;
    if (state.busy) return;
    var flush = function() {
      state.busy = true;
      state.dirty = false;
      FS.syncfs(false, function(error) {
        state.busy = false;
        if (error) console.warn('Prismatch analytics persistence failed', error);
        if (state.dirty) flush();
      });
    };
    flush();
  }
});
