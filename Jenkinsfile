pipeline {
    agent any
    
    environment {
        ANDROID_HOME = 'D:\\NVPACK\\android-sdk-windows'
        PROJECT_PATH = 'E:\\cocos2d-x\\tests\\cpp-tests\\proj.android-studio'
        OUTPUT_DIR = 'D:\\apk'
        // 添加Gradle优化参数
        GRADLE_OPTS = '-Dorg.gradle.daemon=false -Dorg.gradle.vfs.watch=false -Dorg.gradle.caching=true'
        JAVA_OPTS = '-Xmx4096m -XX:MaxMetaspaceSize=1024m'
    }
    
    triggers {
        pollSCM('H/5 * * * *')  // 每5分钟检查一次Git更新
    }
    
    stages {
        stage('Checkout') {
            steps {
                git branch: 'cocos2d-x-3.10', 
                url: 'https://github.com/zhangenping/cocos2d-x.git',
                credentialsId: '1520153104@qq.com'
            }
        }
        
        stage('Clean Project') {
            steps {
                script {
                    dir('proj.android-studio') {
                        // 只清理，不构建
                        bat './gradlew clean --no-daemon --console=plain'
                    }
                }
            }
        }
        
        stage('Build APK') {
            steps {
                script {
                    dir('proj.android-studio') {
                        // 使用优化参数一次性构建APK
                        bat '''
                            ./gradlew assembleDebug \
                                --no-daemon \
                                --no-watch-fs \
                                --build-cache \
                                --parallel \
                                --console=plain \
                                -Dorg.gradle.vfs.watch=false \
                                -Dorg.gradle.caching=true
                        '''
                    }
                }
            }
        }
        
        stage('Find and Copy APK') {
            steps {
                script {
                    dir('proj.android-studio') {
                        bat '''
                            echo "=== 开始查找APK文件 ==="
                            echo "工作目录:"
                            cd
                            
                            # 首先检查标准路径
                            if exist "app\\build\\outputs\\apk\\debug\\*.apk" (
                                echo "在标准路径找到APK文件"
                                dir /b "app\\build\\outputs\\apk\\debug\\*.apk"
                                copy "app\\build\\outputs\\apk\\debug\\*.apk" "%OUTPUT_DIR%\\"
                                echo "APK已复制到: %OUTPUT_DIR%"
                            ) else (
                                echo "在标准路径未找到APK，搜索整个项目..."
                                dir /b/s *.apk
                                
                                # 如果找到APK文件，复制到输出目录
                                for /r %%i in (*.apk) do (
                                    echo "找到APK文件: %%i"
                                    copy "%%i" "%OUTPUT_DIR%\\"
                                    echo "已复制: %%i -> %OUTPUT_DIR%"
                                )
                            )
                            
                            # 检查是否成功复制
                            if exist "%OUTPUT_DIR%\\*.apk" (
                                echo "=== APK复制成功 ==="
                                dir /b "%OUTPUT_DIR%\\*.apk"
                            ) else (
                                echo "!!! 错误: 未找到或复制APK文件 !!!"
                                exit 1
                            )
                        '''
                    }
                }
            }
        }
        
        stage('Archive APK') {
            steps {
                script {
                    // 归档APK文件到Jenkins
                    bat '''
                        if exist "%OUTPUT_DIR%\\*.apk" (
                            echo "归档APK文件..."
                            copy "%OUTPUT_DIR%\\*.apk" "%WORKSPACE%\\"
                        )
                    '''
                    archiveArtifacts artifacts: '*.apk', fingerprint: true
                }
            }
        }
    }
    
    post {
        always {
            // 清理临时文件
            script {
                bat '''
                    echo "清理临时文件..."
                    if exist "%WORKSPACE%\\*.apk" del "%WORKSPACE%\\*.apk"
                '''
            }
        }
        success {
            emailext (
                subject: "构建成功: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """
                <h3>构建成功!</h3>
                <p>项目: ${env.JOB_NAME}</p>
                <p>构建号: #${env.BUILD_NUMBER}</p>
                <p>APK位置: ${env.OUTPUT_DIR}</p>
                <p>查看详情: <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                """,
                to: "1520153104@qq.com",
                mimeType: "text/html"
            )
        }
        failure {
            emailext (
                subject: "构建失败: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """
                <h3>构建失败!</h3>
                <p>项目: ${env.JOB_NAME}</p>
                <p>构建号: #${env.BUILD_NUMBER}</p>
                <p>请检查构建日志: <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                """,
                to: "1520153104@qq.com",
                mimeType: "text/html"
            )
        }
    }
}