mergeInto(LibraryManager.library, {
  WebGLCopyToClipboard: function (textPtr) {
    var text = UTF8ToString(textPtr);
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text);
      }
    } catch (e) {
      console.warn("Clipboard is not available", e);
    }
  }
});
