APP_STL := c++_shared
APP_CPPFLAGS := -frtti -fexceptions -DCC_ENABLE_CHIPMUNK_INTEGRATION=1 -std=c++14 -fsigned-char
APP_CPPFLAGS += -Wno-register
APP_CPPFLAGS += -Dgetdtablesize\(\)=1024
APP_LDFLAGS := -latomic
APP_ABI := armeabi-v7a x86
APP_PLATFORM := android-21
APP_SHORT_COMMANDS := true
NDK_DEBUG := 1
APP_OPTIM := debug

ifeq ($(NDK_DEBUG),1)
  APP_CPPFLAGS += -DCOCOS2D_DEBUG=1
  APP_OPTIM := debug
else
  APP_CPPFLAGS += -DNDEBUG
  APP_OPTIM := release
endif