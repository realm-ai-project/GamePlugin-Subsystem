cd %4
realm-tune --config-path=%1 --env-path=%2 --behavior-name=%3
cd runs

@echo off
for /D %%a in (*) do (cd "%%~a" && GOTO train)

:train
  cd best_trial
  for %%f in (.\*) do (
    mlagents-learn "%%~f" --run-id="Best"
    goto posttrain
  )

:posttrain
echo Done!
cd ..
cd ..
cd ..
echo Results in %cd%
@echo on
	