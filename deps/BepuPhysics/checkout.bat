@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/bepu/bepuphysics2.git ../../externals/bepuphysics2
if NOT ERRORLEVEL 0 pause
