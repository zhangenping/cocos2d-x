package org.cocos2dx.cpp_tests;

import android.app.Application;
import android.os.Build;
import android.util.Log;
import com.tencent.rdelivery.dependency.AbsLog;
import com.tencent.rfix.entry.RFixApplicationLike;
import com.tencent.rfix.lib.RFixListener;
import com.tencent.rfix.lib.config.PatchConfig;
import com.tencent.rfix.lib.entity.RFixPatchResult;
import com.tencent.shiply.integration.ShiplyIntegrationHelper;
import com.tencent.shiply.integration.ShiplyParams;

/**
 * Created by raymondhu on 2025/4/23
 */
public class InitUtil {
    private static final String TAG = "InitUtil";
    public static void initShiplySDK(Application application, RFixApplicationLike rFixApplicationLike) {

        String appId = "2396be5323"; // 在shiply前端页面申请的项目Android产品的appid
        String appKey = "e086553c-2e41-449e-a8a5-8cebdc3a00fb"; // 在shiply前端页面申请的项目Android产品的appkey
        String bundleID = com.tencent.bugly.demo.BuildConfig.APPLICATION_ID;
        Log.d(TAG, "initShiplySDK : " + appId + ", " + appKey + ", " + bundleID);

        // 宿主是否是debug包
        boolean isDebugPackage = com.tencent.bugly.demo.BuildConfig.DEBUG;

        ShiplyParams shiplyParams = new ShiplyParams.Builder()
                .appId(appId)
                .appKey(appKey)
                .userId("123321")
                .deviceId("deviceXXX")
                .hostAppVersion(com.tencent.bugly.demo.BuildConfig.VERSION_NAME)
                .isDebugPackage(isDebugPackage)
                .devModel(Build.MODEL)
                .devManufacturer(Build.MANUFACTURER)
                .androidSystemVersion(String.valueOf(Build.VERSION.SDK_INT))
                .logImpl(new CustomLogger())
                .build();

        ShiplyIntegrationHelper.INSTANCE.initialize(application, shiplyParams);
        ShiplyIntegrationHelper.INSTANCE.getRdeliveryInstance();

        ShiplyIntegrationHelper.INSTANCE.getReshubInstance();

        ShiplyIntegrationHelper.INSTANCE.initUpgrade(ShiplyIntegrationHelper.UpgradeDiffType.OriginPackage);
        ShiplyIntegrationHelper.INSTANCE.getUpgradeInstance();

        if (rFixApplicationLike != null) {
            ShiplyIntegrationHelper.INSTANCE.initRFix(rFixApplicationLike, new RFixListener() {
                @Override
                public void onConfig(boolean b, int i, PatchConfig patchConfig) {
                    Log.d(TAG, "onConfig : " + b + ", " + i);
                }

                @Override
                public void onDownload(boolean b, int i, PatchConfig patchConfig, String s) {
                    Log.d(TAG, "onDownload : " + b + ", " + i + ", " + s);
                }

                @Override
                public void onInstall(boolean b, int i, RFixPatchResult rFixPatchResult) {
                    Log.d(TAG, "onInstall : " + b + ", " + i);
                }
            });
            ShiplyIntegrationHelper.INSTANCE.getRFixInstance();
        }
    }


    public static class CustomLogger extends AbsLog {
        @Override
        public void log(String tag, Level logLevel, String msg) {
            String newTag = "ShiplyDemo_" + tag;
            if (logLevel == Level.VERBOSE) {
                Log.v(newTag, msg != null ? msg : "");
            } else if (logLevel == Level.DEBUG) {
                Log.d(newTag, msg != null ? msg : "");
            } else if (logLevel == Level.INFO) {
                Log.i(newTag, msg != null ? msg : "");
            } else if (logLevel == Level.WARN) {
                Log.w(newTag, msg != null ? msg : "");
            } else if (logLevel == Level.ERROR) {
                Log.e(newTag, msg != null ? msg : "");
            }
        }

        @Override
        public void log(String tag, Level logLevel, String msg, Throwable throwable) {
            String newTag = "ShiplyDemo_" + tag;
            if (logLevel == Level.VERBOSE) {
                Log.v(newTag, msg, throwable);
            } else if (logLevel == Level.DEBUG) {
                Log.d(newTag, msg, throwable);
            } else if (logLevel == Level.INFO) {
                Log.i(newTag, msg, throwable);
            } else if (logLevel == Level.WARN) {
                Log.w(newTag, msg, throwable);
            } else if (logLevel == Level.ERROR) {
                Log.e(newTag, msg, throwable);
            }
        }
    }
}
