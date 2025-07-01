// Assets/Plugins/MobileDetection.jslib
var MobileDetection = {
   IsMobile: function() {
      return Module.SystemInfo.mobile;
   }
};
mergeInto(LibraryManager.library, MobileDetection);
