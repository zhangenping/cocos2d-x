package org.cocos2dx.cpp_tests;

import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.Build;
import android.util.Log;

import com.tencent.bugly.crashreport.CrashReport;

/**
 * Bugly 异常监控管理器
 */
public class BuglyManager {
    private static final String TAG = "BuglyManager";
    private static boolean isInitialized = false;
    private static final String BUGLY_APP_ID = "7cedd98b5b";

    /**
     * 初始化 Bugly
     */
    public static void init(Context context) {
        if (isInitialized) {
            Log.d(TAG, "Bugly 已经初始化过");
            return;
        }

        try {
            Log.d(TAG, "开始初始化 Bugly, AppId: " + BUGLY_APP_ID);

            if (BUGLY_APP_ID.equals("替换为你的APP_ID")) {
                Log.e(TAG, "请先配置正确的 Bugly AppId");
                return;
            }

            // 使用 CrashReport 进行初始化
            CrashReport.UserStrategy strategy = new CrashReport.UserStrategy(context);
            strategy.setAppChannel(getChannel(context));
            strategy.setAppVersion(getVersionName(context));
            strategy.setAppPackageName(context.getPackageName());
            strategy.setUploadProcess(true);
            strategy.setEnableNativeCrashMonitor(true); // 重要：开启 Native 监控
            // 需要添加符号表配置
            strategy.setAppReportDelay(15000); // 延迟上报，等待符号表
            strategy.setEnableCatchAnrTrace(true); // 捕获 ANR 堆栈


            // 设置 JNI 监控
            CrashReport.setIsDevelopmentDevice(context, BuildConfig.DEBUG);

            // 对于 Cocos2d-x，需要设置 so 文件路径
            setupNativeSymbols(context);

            // 初始化
            CrashReport.initCrashReport(context, BUGLY_APP_ID, BuildConfig.DEBUG, strategy);

            // 设置用户ID
            CrashReport.setUserId("user_" + System.currentTimeMillis());

            // 添加自定义数据
            addCustomData(context);

            enableNativeCrashMonitorImmediately();

            isInitialized = true;
            Log.d(TAG, "Bugly 初始化完成");

            // 打印设备信息
            logDeviceInfo(context);

        } catch (Exception e) {
            Log.e(TAG, "Bugly 初始化失败", e);
            isInitialized = false;
        }
    }

    private static void enableNativeCrashMonitorImmediately() {
        try {
            // 强制加载 Bugly Native 库
            System.loadLibrary("Bugly");
            Log.d(TAG, "Bugly Native 库加载完成");

            // 设置 Native 异常回调
            //setupNativeCallback();

        } catch (Throwable e) {
            Log.e(TAG, "启用 Native 监控失败", e);
        }
    }

    private static void setupNativeSymbols(Context context) {
        try {
            // 获取 so 文件路径
            String soPath = context.getApplicationInfo().nativeLibraryDir;
            Log.d(TAG, "Native library path: " + soPath);

        } catch (Exception e) {
            Log.e(TAG, "设置 Native 符号表路径失败", e);
        }
    }

    public static void uploadSymbolsManually(Context context) {
        if (!BuildConfig.DEBUG) {
            return;
        }

        try {
            String soPath = context.getApplicationInfo().nativeLibraryDir;
            Log.d(TAG, "手动上传符号表，路径: " + soPath);

            // 触发符号表上传
            CrashReport.testNativeCrash();

        } catch (Exception e) {
            Log.e(TAG, "手动上传符号表失败", e);
        }
    }

    /**
     * 添加自定义数据
     */
    private static void addCustomData(Context context) {
        try {
            CrashReport.putUserData(context, "DeviceModel", Build.MODEL);
            CrashReport.putUserData(context, "AndroidVersion", Build.VERSION.RELEASE);

            CrashReport.putUserData(context, "Manufacturer", Build.MANUFACTURER);
            CrashReport.putUserData(context, "AppVersion", getVersionName(context));
            CrashReport.putUserData(context, "Channel", getChannel(context));

            Log.d(TAG, "自定义数据添加完成");
        } catch (Exception e) {
            Log.e(TAG, "添加自定义数据失败", e);
        }
    }

    /**
     * 打印设备信息
     */
    private static void logDeviceInfo(Context context) {
        Log.d(TAG, "=== 设备信息 ===");
        Log.d(TAG, "设备型号: " + Build.MODEL);
        Log.d(TAG, "Android版本: " + Build.VERSION.RELEASE);
        Log.d(TAG, "SDK版本: " + Build.VERSION.SDK_INT);
        Log.d(TAG, "应用版本: " + getVersionName(context));
        Log.d(TAG, "渠道: " + getChannel(context));
        Log.d(TAG, "=================");
    }

    /**
     * 获取应用渠道
     */
    private static String getChannel(Context context) {
        try {
            PackageManager pm = context.getPackageManager();
            ApplicationInfo appInfo = pm.getApplicationInfo(context.getPackageName(),
                    PackageManager.GET_META_DATA);
            return appInfo.metaData.getString("BUGLY_APP_CHANNEL", "official");
        } catch (Exception e) {
            Log.w(TAG, "获取渠道信息失败，使用默认渠道");
            return "official";
        }
    }

    /**
     * 获取版本名称
     */
    private static String getVersionName(Context context) {
        try {
            PackageInfo packageInfo = context.getPackageManager()
                    .getPackageInfo(context.getPackageName(), 0);
            return packageInfo.versionName;
        } catch (Exception e) {
            Log.w(TAG, "获取版本信息失败，使用默认版本");
            return "1.0.0";
        }
    }

    /**
     * 测试 Java 崩溃（仅调试用）
     */
    public static void testJavaCrash() {
        if (!BuildConfig.DEBUG) {
            Log.w(TAG, "生产环境不允许测试崩溃");
            return;
        }

        Log.w(TAG, "触发测试崩溃...");
        throw new RuntimeException("这是 Bugly 的测试 Java 崩溃");
    }

    /**
     * 测试 Native 崩溃（仅调试用）
     */
    public static void testNativeCrash() {
        if (!BuildConfig.DEBUG) {
            Log.w(TAG, "生产环境不允许测试崩溃");
            return;
        }

        Log.w(TAG, "触发测试 Native 崩溃...");
        CrashReport.testNativeCrash();
    }

    /**
     * 手动上报异常
     */
    public static void reportException(Throwable throwable, String message) {
        if (!isInitialized) {
            Log.w(TAG, "Bugly 未初始化，无法上报异常");
            return;
        }

        try {
            CrashReport.postCatchedException(throwable);
            Log.d(TAG, "异常已上报: " + message);
        } catch (Exception e) {
            Log.e(TAG, "上报异常失败", e);
        }
    }

    /**
     * 设置用户标识
     */
    public static void setUserIdentifier(String userId) {
        if (!isInitialized) {
            Log.w(TAG, "Bugly 未初始化，无法设置用户标识");
            return;
        }

        try {
            CrashReport.setUserId(userId);
            Log.d(TAG, "用户标识已设置: " + userId);
        } catch (Exception e) {
            Log.e(TAG, "设置用户标识失败", e);
        }
    }

    /**
     * 检查 Bugly 是否初始化成功
     */
    public static boolean isBuglyInitialized() {
        return isInitialized;
    }


}