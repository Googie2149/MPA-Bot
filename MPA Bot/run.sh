#!/bin/bash

RESTARTS=0
UPDATE=0
while true; do
	dotnet run
	if [ $? -eq 0 ] #Intentional close, exit script
		echo "Exited cleanly."
		exit 0
	elif [ $? -eq 5 ] #Pull latest code and rebuild
		cd ..
		git pull
		cd -
		RESTARTS=0
	elif [ $? -eq 12 ] #Escaping a deadlock
		RESTARTS=$((RESTARTS + 1))
		UPDATE=0
		if [ $RESTARTS -ge 6]
			echo "Too many failed restart attempts, Discord is likely having massive issues."
			exit 12 
		fi
	else
		RESTARTS=$((RESTARTS + 1))
		UPDATE=0
		sleep 30s
		if [ $RESTARTS -ge 12]
			echo "$?: Too many failed restart attempts"
			exit 1
		fi
	fi
done
