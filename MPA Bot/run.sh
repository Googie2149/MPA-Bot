#!/bin/bash -x

RESTARTS=0
EXIT=-1
rm ./update
while true; do
	dotnet run
	EXIT=$?
	if [ $EXIT -eq 0 ]; then 
		echo "Exited cleanly."
		exit 0
	elif [ $EXIT -eq 4 ]; then
		echo "Restarting..."
	elif [ $EXIT -eq 5 ]; then
		echo "Pulling latest update..."
		cd ..
		git pull
		dotnet restore
		cd -
		RESTARTS=0
		echo "Restarting..."
	elif [ $EXIT -eq 12 ]; then 
		RESTARTS=$((RESTARTS + 1))
		if [ $RESTARTS -ge 6 ]; then
			echo "Too many failed restart attempts, Discord is likely having massive issues."
			exit 12 
		fi;
	else
		RESTARTS=$((RESTARTS + 1))
		UPDATE=0
		sleep 30s
		if [ $RESTARTS -ge 12 ]; then
			echo "$?: Too many failed restart attempts"
			exit 1
		fi;
	fi;
done
