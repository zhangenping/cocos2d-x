pipeline {
    agent any
    
    environment {
        ANDROID_HOME = 'D:\\NVPACK\\android-sdk-windows'
        PROJECT_PATH = 'E:\\cocos2d-x\\tests\\cpp-tests\\proj.android-studio'
        OUTPUT_DIR = 'D:\\apk'
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
        
        stage('Build APK') {
            steps {
                script {
                    // 进入Android项目目录
                    dir(env.PROJECT_PATH) {
                        // 清理项目
                        bat './gradlew clean'
                        
                        // 构建APK
                        bat './gradlew assembleDebug'
                    }
                }
            }
        }
        
        stage('Copy APK') {
            steps {
                script {
                    // 查找APK文件
                    def files = bat(script: 'dir /b/s *.apk', returnStdout: true).trim()
					if (files) 
					{
						// 复制到目标目录
						bat "xcopy \"${apkFiles[0].path}\" \"${env.OUTPUT_DIR}\" /Y"
						
						// 记录构建信息
						echo "APK已生成并复制到: ${env.OUTPUT_DIR}"
						echo "文件名: ${apkFiles[0].name}"
					}
                }
            }
        }
    }
    
    post {
        always {
            emailext (
                subject: "构建通知: ${currentBuild.fullDisplayName}",
                body: "构建结果: ${currentBuild.result}\n查看详情: ${env.BUILD_URL}",
                to: "1520153104@qq.com"
            )
        }
    }
}