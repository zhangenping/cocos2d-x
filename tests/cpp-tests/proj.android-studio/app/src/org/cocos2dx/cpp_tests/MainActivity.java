package org.cocos2dx.cpp_tests;

import com.tencent.bugly.crashreport.CrashReport;
import com.tencent.rdelivery.data.RDeliveryData;
import com.tencent.rdelivery.listener.FullReqResultListener;
import com.tencent.rdelivery.listener.MultiKeysReqResultListener;
import com.tencent.rdelivery.listener.SingleReqResultListener;
import com.tencent.rdelivery.reshub.api.IRes;
import com.tencent.rdelivery.reshub.api.IResCallback;
import com.tencent.rdelivery.reshub.api.IResLoadError;
import com.tencent.shiply.integration.ShiplyIntegrationHelper;

import android.os.Bundle;
import android.text.TextUtils;
import android.util.Log;

import org.cocos2dx.lib.Cocos2dxActivity;
import org.cocos2dx.lib.Cocos2dxGLSurfaceView;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Arrays;
import java.util.List;

import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import com.tencent.rdelivery.reshub.api.IRes;
import com.tencent.rdelivery.reshub.api.IResCallback;
import com.tencent.rdelivery.reshub.api.IResHub;
import com.tencent.rdelivery.reshub.api.IResLoadError;
import com.tencent.shiply.integration.ShiplyIntegrationHelper;


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

    /**TDS-shiply config配置*/
    private void requestSingleConfig(String key) {
        Log.d(TAG, "requestSingleConfig key = " + key);
        if (TextUtils.isEmpty(key)) {
            return;
        }
        ShiplyIntegrationHelper.INSTANCE.getRdeliveryInstance().requestSingleRemoteDataByKey(key, new SingleReqResultListener() {
            @Override
            public void onSuccess(RDeliveryData data) {
                Log.d(TAG, "requestSingleRemoteDataByKey onSuccess data = " + data);
            }

            @Override
            public void onFail(String reason) {

            }
        });
    }

    private void requestMultiConfig(String key) {
        Log.d(TAG, "requestMultiConfig key = " + key);
        if (TextUtils.isEmpty(key)) {
            return;
        }
        List<String> keyList = Arrays.asList(key.split(","));
        ShiplyIntegrationHelper.INSTANCE.getRdeliveryInstance().requestMultiRemoteData(keyList, new MultiKeysReqResultListener() {
            @Override
            public void onSuccess(List<RDeliveryData> datas) {

            }

            @Override
            public void onFail(String reason) {

            }
        });
    }

    private void requestFullConfig() {
        ShiplyIntegrationHelper.INSTANCE.getRdeliveryInstance().requestFullRemoteData(new FullReqResultListener() {
            @Override
            public void onSuccess() {
                Log.d(TAG, "manual full req result, onSuccess without arg ");
            }

            @Override
            public void onSuccess(List<RDeliveryData> remainedDatas, List<RDeliveryData> updatedDatas, List<RDeliveryData> deletedDatas) {
                onSuccess();
                Log.d(TAG, "manual full req result, onSuccess with arg, updatedDatas = " + updatedDatas + " remainedDatas = " + remainedDatas);
            }

            @Override
            public void onFail(String reason) {

            }
        });
    }

    /**TDS-shiply 资源拉取*/
    private IResHub reshub;
    private void loadLatestResource(String resId) {
        reshub.loadLatest(resId, new IResCallback() {
            @Override
            public void onProgress(float progress) {
                Log.d("ResHubLoad", "资源加载进度: " + progress);
            }

            @Override
            public void onComplete(boolean isSuccess, IRes result, IResLoadError error) {
                if (isSuccess) {
                    Log.d("ResHubLoad", "资源拉取成功");

                } else {
                    Log.e("ResHubLoad", "异步最新资源拉取失败");
                }
            }
        });
    }

    private void loadResource(String resId) {
        reshub.load(resId, new IResCallback() {
            @Override
            public void onProgress(float progress) {
                Log.d("ResHubLoad", "资源加载进程: " + progress);
            }

            @Override
            public void onComplete(boolean isSuccess, IRes result, IResLoadError error) {
                if (isSuccess) {
                    Log.d("ResHubLoad", "异步资源拉取成功");
                    if (result != null) {
                        // 获取默认存储路径
                        String originalPath = result.getLocalPath();

                        // 复制到外部存储
                        copyToExternalStorage(originalPath, resId);
                    }
                } else {
                    Log.e("ResHubLoad", "异步资源拉取失败");
                }
            }
        });
    }

    private void copyToExternalStorage(String originalPath, String resId) {
        try {
            File originalDir = new File(originalPath);

            // 检查原始路径是否是目录
            if (!originalDir.exists()) {
                Log.e("ResHubLoad", "原始目录不存在: " + originalPath);
                return;
            }

            if (!originalDir.isDirectory()) {
                Log.e("ResHubLoad", "原始路径不是目录: " + originalPath);
                return;
            }

            // 创建外部存储目标目录
            File externalDir = getExternalFilesDir("reshub_downloads");
            File targetDir = new File(externalDir, resId);
            if (!targetDir.exists()) {
                targetDir.mkdirs();
            }

            // 复制目录中的所有文件
            boolean copySuccess = copyDirectory(originalDir, targetDir);

            if (copySuccess) {
                // 更新UI显示新路径
                Log.d("ResHubLoad", "目录已复制到: " + targetDir.getAbsolutePath());
            } else {
            }

        } catch (Exception e) {
            Log.e("ResHubLoad", "复制目录失败", e);
        }
    }

    private boolean copyDirectory(File sourceDir, File targetDir) {
        if (!sourceDir.isDirectory()) {
            return false;
        }

        boolean allSuccess = true;

        // 获取目录中的所有文件和子目录
        File[] files = sourceDir.listFiles();
        if (files == null) {
            Log.e("ResHubLoad", "无法读取目录内容: " + sourceDir.getAbsolutePath());
            return false;
        }

        for (File file : files) {
            File targetFile = new File(targetDir, file.getName());

            if (file.isDirectory()) {
                // 如果是子目录，递归复制
                if (!targetFile.exists()) {
                    targetFile.mkdirs();
                }
                boolean subDirSuccess = copyDirectory(file, targetFile);
                if (!subDirSuccess) {
                    allSuccess = false;
                }
            } else {
                // 如果是文件，复制文件
                boolean fileSuccess = copyFile(file, targetFile);
                if (!fileSuccess) {
                    allSuccess = false;
                }
            }
        }

        return allSuccess;
    }

    private boolean copyFile(File sourceFile, File targetFile) {
        try {
            FileInputStream in = new FileInputStream(sourceFile);
            FileOutputStream out = new FileOutputStream(targetFile);

            byte[] buffer = new byte[1024];
            int length;
            while ((length = in.read(buffer)) > 0) {
                out.write(buffer, 0, length);
            }

            in.close();
            out.close();

            Log.d("ResHubLoad", "文件复制成功: " + sourceFile.getName() + " -> " + targetFile.getAbsolutePath());
            return true;

        } catch (IOException e) {
            Log.e("ResHubLoad", "复制文件失败: " + sourceFile.getName(), e);
            return false;
        }
    }

    private void getResource(String resId) {
        IRes res = reshub.get(resId, false);
        if (res != null) {
            Log.d("ResHubGet", "同步资源获取成功: " + res.getLocalPath());
            updateResourceContent(res);
        } else {
            Log.e("ResHubGet", "同步资源获取失败");
        }
    }

    private void getLatestResource(String resId) {
        IRes res = reshub.getLatest(resId, false);
        if (res != null) {
            Log.d("ResHubGet", "成功获取同步最新资源: " + res.getLocalPath());
            updateResourceContent(res);
        } else {
            Log.e("ResHubGet", "获取同步最新资源失败");
        }
    }

    private void updateResourceContent(IRes res) {
        String content = "ResId: " + res.getResId() + "\n"
                + "LocalPath: " + res.getLocalPath() + "\n"
                + "Version: " + res.getVersion() + "\n"
                + "Size: " + res.getSize() + "\n"
                + "MD5: " + res.getMd5() + "\n"
                + "DownloadUrl: " + res.getDownloadUrl() + "\n"
                + "FileExtra: " + res.getFileExtra() + "\n"
                + "ResType: " + res.getResType() + "\n"
                + "Description: " + res.getDescription() + "\n"
                + "TaskId: " + res.getTaskId();
    }
}
