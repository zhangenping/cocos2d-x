package org.cocos2dx.cpp_tests;

import com.tencent.bugly.crashreport.CrashReport;

import android.os.Bundle;
import android.util.Log;

import org.cocos2dx.lib.Cocos2dxActivity;
import org.cocos2dx.lib.Cocos2dxGLSurfaceView;



/**
 * 简单测试crash
 * 注：如想查看crash日志，需先到http://bugly.qq.com/注册app，并配置appID，之后就可以在bugly查看到日志啦
 * @author wenjiewu
 * @date 2016/5/23
 */
public class MainActivity extends Cocos2dxActivity {
    private static final String TAG = "AppActivity";
    private static boolean isBuglyInitialized = false;
    static {
        Log.d(TAG, "开始加载 native 库...");
        try {
            // 加载实际的库名
            System.loadLibrary("cpp_tests");
            Log.d(TAG, "cpp_tests 库加载成功");
        } catch (UnsatisfiedLinkError e) {
            Log.e(TAG, "cpp_tests 库加载失败: " + e.getMessage());
        }
    }

    @Override
    protected void onLoadNativeLibraries() {
        try {
            // 重写这个方法，防止父类加载错误的库
            System.loadLibrary("cpp_tests");
            Log.d(TAG, "通过 onLoadNativeLibraries 加载成功");
        } catch (UnsatisfiedLinkError e) {
            Log.e(TAG, "加载失败: " + e.getMessage());
            throw e;
        }
    }

    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }
}
