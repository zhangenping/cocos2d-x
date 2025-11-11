pipeline {
    agent any
    
    environment {
        ANDROID_HOME = 'D:\\NVPACK\\android-sdk-windows'
        PROJECT_PATH = 'E:\\cocos2d-x\\tests\\cpp-tests\\proj.android-studio'
        OUTPUT_DIR = 'D:\\apk'
        // 优化Gradle和Java参数
        GRADLE_OPTS = '-Dorg.gradle.daemon=true -Dorg.gradle.parallel=true -Dorg.gradle.caching=true'
        JAVA_OPTS = '-Xmx4096m -XX:MaxMetaspaceSize=1024m -XX:+UseG1GC'
    }
    
    triggers {
        pollSCM('H/5 * * * *')
    }
    
    stages {
        stage('Check SCM Changes') {
            steps {
                script {
                    // 检查是否有实际的文件变更，避免无谓构建
                    def changes = bat(script: 'git diff --name-only HEAD~1 HEAD', returnStdout: true).trim()
                    if (!changes) {
                        echo "没有检测到代码变更，跳过构建"
                        currentBuild.result = 'SUCCESS'
                    }
                }
            }
        }
        
        stage('Prepare Environment') {
            when {
                expression { currentBuild.result != 'SUCCESS' }
            }
            steps {
                script {
                    bat """
                        if not exist "${OUTPUT_DIR}" mkdir "${OUTPUT_DIR}"
                    """
                }
            }
        }
        
        stage('Incremental Build') {
            when {
                expression { currentBuild.result != 'SUCCESS' }
            }
            steps {
                script {
                    dir(env.PROJECT_PATH) {
                        // 使用增量构建，只编译变更的部分
                        bat """
                            gradlew assembleDebug ^
                                --configure-on-demand ^
                                --parallel ^
                                --build-cache ^
                                --no-rebuild ^
                                --console=plain
                        """
                    }
                }
            }
        }
        
        stage('Quick APK Copy') {
            when {
                expression { currentBuild.result != 'SUCCESS' }
            }
            steps {
                script {
                    dir(env.PROJECT_PATH) {
                        bat """
                            if exist "app\\\\build\\\\outputs\\\\apk\\\\debug\\\\*.apk" (
                                echo "快速复制APK文件..."
                                copy "app\\\\build\\\\outputs\\\\apk\\\\debug\\\\*.apk" "${OUTPUT_DIR}\\\\" >nul
                                echo "APK复制完成"
                            ) else (
                                echo "错误: 未找到APK文件"
                                exit 1
                            )
                        """
                    }
                }
            }
        }
    }
    
    post {
        always {
            script {
                bat """
                    echo "=== 构建统计 ==="
                    echo "构建时间: ${currentBuild.durationString}"
                    echo "构建结果: ${currentBuild.result}"
                """
            }
        }
        success {
            script {
                echo "?? 构建完成 - 使用增量构建优化"
                // 简化的成功通知，避免邮件发送失败导致构建失败
            }
        }
        unsuccessful {
            script {
                echo "? 构建失败 - 请检查日志"
            }
        }
    }
    
    options {
        timeout(time: 20, unit: 'MINUTES') // 设置20分钟超时
        retry(1) // 失败时重试1次
        timestamps() // 添加时间戳
    }
}