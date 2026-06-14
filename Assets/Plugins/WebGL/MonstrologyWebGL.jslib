mergeInto(LibraryManager.library, {
  $MonstrologyGetSafeInset: function (side) {
    try {
      var probeId = "monstrology-safe-area-probe";
      var probe = document.getElementById(probeId);

      if (!probe) {
        probe = document.createElement("div");
        probe.id = probeId;
        probe.style.position = "fixed";
        probe.style.left = "0";
        probe.style.top = "0";
        probe.style.width = "0";
        probe.style.height = "0";
        probe.style.visibility = "hidden";
        probe.style.pointerEvents = "none";
        probe.style.paddingLeft = "env(safe-area-inset-left, 0px)";
        probe.style.paddingRight = "env(safe-area-inset-right, 0px)";
        probe.style.paddingTop = "env(safe-area-inset-top, 0px)";
        probe.style.paddingBottom = "env(safe-area-inset-bottom, 0px)";

        var parent = document.body || document.documentElement;
        if (!parent) {
          return 0;
        }

        parent.appendChild(probe);
      }

      var computed = window.getComputedStyle(probe);
      var value = 0;
      switch (side) {
        case 0:
          value = parseFloat(computed.paddingLeft) || 0;
          break;
        case 1:
          value = parseFloat(computed.paddingRight) || 0;
          break;
        case 2:
          value = parseFloat(computed.paddingTop) || 0;
          break;
        case 3:
          value = parseFloat(computed.paddingBottom) || 0;
          break;
        default:
          return 0;
      }

      var viewport = window.visualViewport;
      var width = viewport && viewport.width
        ? viewport.width
        : document.documentElement.clientWidth || window.innerWidth || 1;
      var height = viewport && viewport.height
        ? viewport.height
        : document.documentElement.clientHeight || window.innerHeight || 1;
      var divisor = side === 0 || side === 1 ? width : height;
      if (divisor <= 0) {
        return 0;
      }

      return Math.max(0, Math.min(1, value / divisor));
    } catch (error) {
      console.warn("[MonstrologyWebGL] Safe-area fallback:", error);
      return 0;
    }
  },

  Monstrology_IsMobileUserAgent: function () {
    try {
      return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i
        .test(navigator.userAgent || "") ? 1 : 0;
    } catch (error) {
      return 0;
    }
  },

  Monstrology_RequestFullscreen: function () {
    try {
      var element = document.getElementById("unity-container") ||
        document.getElementById("unity-canvas") ||
        document.documentElement;
      var request = element.requestFullscreen ||
        element.webkitRequestFullscreen ||
        element.webkitRequestFullScreen ||
        element.mozRequestFullScreen ||
        element.msRequestFullscreen;
      if (!request) {
        return 0;
      }

      var result = request.call(element);
      if (result && typeof result.catch === "function") {
        result.catch(function () {});
      }

      return 1;
    } catch (error) {
      return 0;
    }
  },

  Monstrology_GetSafeInsetLeft__deps: [
    "$MonstrologyGetSafeInset"
  ],
  Monstrology_GetSafeInsetLeft: function () {
    return MonstrologyGetSafeInset(0);
  },

  Monstrology_GetSafeInsetRight__deps: [
    "$MonstrologyGetSafeInset"
  ],
  Monstrology_GetSafeInsetRight: function () {
    return MonstrologyGetSafeInset(1);
  },

  Monstrology_GetSafeInsetTop__deps: [
    "$MonstrologyGetSafeInset"
  ],
  Monstrology_GetSafeInsetTop: function () {
    return MonstrologyGetSafeInset(2);
  },

  Monstrology_GetSafeInsetBottom__deps: [
    "$MonstrologyGetSafeInset"
  ],
  Monstrology_GetSafeInsetBottom: function () {
    return MonstrologyGetSafeInset(3);
  }
});
