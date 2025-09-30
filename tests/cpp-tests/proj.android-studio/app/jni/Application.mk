APP_STL := c++_shared
APP_CPPFLAGS := -frtti -DCC_ENABLE_CHIPMUNK_INTEGRATION=1 -std=c++14 -fsigned-char
APP_LDFLAGS := -latomic
APP_ABI := armeabi-v7a
# APP_ABI := armeabi-v7a arm64-v8a x86 x86_64
APP_PLATFORM := android-21

ifeq ($(NDK_DEBUG),1)
  APP_CPPFLAGS += -DCOCOS2D_DEBUG=1
  APP_OPTIM := debug
else
  APP_CPPFLAGS += -DNDEBUG
  APP_OPTIM := release
endif