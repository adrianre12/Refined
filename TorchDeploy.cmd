@REM exit 0
REM exit 0
set MOD_NAME=Refined
set MOD_ID=3560877818

set SE_CONTENT_DIR=N:\SteamLibrary\steamapps\workshop\content\244850
set TORCH_CONTENT_DIR=G:\torch-server\Instance\content\244850

set MOD_DIR=%APPDATA%\SpaceEngineers\Mods

rmdir %SE_CONTENT_DIR%\%MOD_ID% /S /Q 
robocopy.exe %MOD_DIR%\%MOD_NAME%\ %SE_CONTENT_DIR%\%MOD_ID%\ "*.*" /S -xf *.sbmi

rmdir %TORCH_CONTENT_DIR%\%MOD_ID% /S /Q 
robocopy.exe %MOD_DIR%\%MOD_NAME%\ %TORCH_CONTENT_DIR%\%MOD_ID%\ "*.*" /S -xf *.sbmi

echo Done