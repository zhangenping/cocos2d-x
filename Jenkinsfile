pipeline {
    agent any
    
    environment {
        ANDROID_HOME = 'D:\\NVPACK\\android-sdk-windows'
        PROJECT_PATH = 'E:\\cocos2d-x\\tests\\cpp-tests\\proj.android-studio'
        OUTPUT_DIR = 'D:\\apk'
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
                    bat "if not exist \"${OUTPUT_DIR}\" mkdir \"${OUTPUT_DIR}\""
                    echo "输出目录准备完成"
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
                        bat """
                            gradlew assembleDebug ^
                                --configure-on-demand ^
                                --parallel ^
                                --build-cache ^
                                --no-rebuild ^
                                --console=plain
                        """
                    }
                    echo "增量构建完成"
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
                        def result = bat(script: """
                            if exist "app\\\\build\\\\outputs\\\\apk\\\\debug\\\\*.apk" (
                                copy "app\\\\build\\\\outputs\\\\apk\\\\debug\\\\*.apk" "${OUTPUT_DIR}\\\\" >nul
                                echo FILE_EXISTS
                            ) else (
                                echo FILE_NOT_FOUND
                            )
                        """, returnStdout: true).trim()
                        
                        if (result.contains("FILE_EXISTS")) {
                            echo "APK复制完成"
                        } else {
                            error "错误: 未找到APK文件"
                        }
                    }
                }
            }
        }
    }
    
    post {
        always {
            echo "=== 构建统计 ==="
            echo "构建时间: ${currentBuild.durationString}"
            echo "构建结果: ${currentBuild.result}"
        }
        success {
            echo "?? 构建完成 - 使用增量构建优化"
        }
        failure {
            echo "? 构建失败 - 请检查日志"
        }
    }
    
    options {
        timeout(time: 20, unit: 'MINUTES')
        retry(1)
        timestamps()
    }
}