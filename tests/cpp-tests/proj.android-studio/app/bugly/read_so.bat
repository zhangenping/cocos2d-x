::该批处理支持单个文件拖入重命名UUID_NAME.SO
::或者传入so文件路径及so文件全名进行UUID重命名
@Echo Off
set rootDir=%~dp0
set soDir=%1
set name=%2

if "%name%"=="" (
	set oldSO=%soDir%
)else (
	set oldSO=..\%soDir%\%name%
)

@echo %oldSO%
set readelf=%READ_ELF%

cd tools

call aarch64-linux-android-readelf.exe %oldSO% -x .note.gnu.build-id > a.txt

For /f "tokens=1* delims=:" %%i in ('Type a.txt^|Findstr /n ".*"') do (
	If "%%i"=="4" (
		set line1=%%j
	)
	If "%%i"=="5" (
		set line2=%%j
	)
)

::预防出现字符串中有需要转义字符，如下
::0x000001fc 82b789b0 ed225879 373ef275 7fc84502 ....."Xy7>.u..E.
set ta=%line1:~22,26%

For /f "tokens=1,2,3,* delims= " %%a in ("%ta%") do (
	set u1=%%a
	set u2=%%b
	set u3=%%c
)
	
set u4=%line2:~13,8%
	
set soID=%u1%%u2%%u3%%u4%
echo %soID%

if "%name%"=="" (
	for %%i in ("%soDir%") do (
		set name=%%~ni.so
	)
)

set soName=%soID%_%name%
echo %soName%
ren %oldSO% %soName%

echo ==========================
del a.txt

cd ..