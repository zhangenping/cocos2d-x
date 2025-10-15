/****************************************************************************
Copyright (c) 2015 Chukong Technologies Inc.

http://www.cocos2d-x.org

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/
package org.cocos2dx.cpp_tests;

import org.cocos2dx.lib.Cocos2dxActivity;
import org.cocos2dx.lib.Cocos2dxGLSurfaceView;
import android.util.Log;
import android.os.Bundle; // 添加这行导入
import android.os.Handler;
import android.os.Looper;
import com.tencent.bugly.crashreport.CrashReport;

public class AppActivity extends Cocos2dxActivity {
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
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        // 初始化 Bugly
        super.onCreate(savedInstanceState);
        Log.d(TAG, "AppActivity onCreate");
        BuglyManager.init(this);
        // 设置全局异常处理器
        setupNativeExceptionHandler();
        setGlobalExceptionHandler();

        Log.d(TAG, "Bugly 初始化状态: " + BuglyManager.isBuglyInitialized());
    }

    private void setupNativeExceptionHandler() {
        Thread.setDefaultUncaughtExceptionHandler(new Thread.UncaughtExceptionHandler() {
            @Override
            public void uncaughtException(Thread thread, Throwable ex) {
                Log.e("Bugly", "捕获到未处理异常: " + ex.getMessage());
                // 确保异常上报
                if (BuglyManager.isBuglyInitialized()) {
                    CrashReport.postCatchedException(ex);
                }
                // 等待上报完成
                try {
                    Thread.sleep(3000);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
                // 退出应用
                System.exit(1);
            }
        });
    }

    @Override
    protected void onResume() {
        super.onResume();

        if (!isBuglyInitialized) {
            // 延迟初始化 Bugly，确保应用已经启动完成
            new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
                @Override
                public void run() {
                    Log.d("Bugly", "延迟初始化 Bugly");
                    BuglyManager.init(AppActivity.this);
                    isBuglyInitialized = true;

                    // 再延迟测试崩溃
                    testBuglyAfterDelay();
                }
            }, 3000); // 应用启动后 3 秒初始化
        }
    }
    private void testBuglyAfterDelay() {
        new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
            @Override
            public void run() {
                Log.w("Bugly", "=== 开始 Bugly 测试 ===");
                // 这里可以触发测试崩溃
                // BuglyManager.testJavaCrash();
            }
        }, 10000); // Bugly 初始化后 10 秒测试
    }

    private void setGlobalExceptionHandler() {
        Thread.setDefaultUncaughtExceptionHandler(new Thread.UncaughtExceptionHandler() {
            @Override
            public void uncaughtException(Thread thread, Throwable ex) {
                Log.e(TAG, "未捕获异常: " + ex.getMessage(), ex);

                // 上报异常到 Bugly
                BuglyManager.reportException(ex, "全局未捕获异常");

                // 直接退出
                finish();
                System.exit(1);
            }
        });
    }

    /**
     * 测试崩溃上报（仅调试用）
     */
    public void testCrashReport() {
        if (BuildConfig.DEBUG) {
            BuglyManager.testJavaCrash();
        }
    }

    /**
     * 设置用户信息（在登录后调用）
     */
    public void setUserInfo(String userId) {
        BuglyManager.setUserIdentifier(userId);
    }
}
