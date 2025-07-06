// WebGLDelay.jslib
mergeInto(LibraryManager.library, {
    DelayAsync: function(milliseconds, callbackPtr) {
        setTimeout(function() {
            dynCall('v', callbackPtr, []);
        }, milliseconds);
    }
});