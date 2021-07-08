FROM mono:6.12.0.107
RUN apt-get update && apt-get install --no-install-recommends -y mono-xsp4 
ADD . /home/forsch
RUN msbuild /home/forsch/Forcsh.sln
RUN mkdir /home/forsch/ForschService/bin/Release/bin
RUN cp /home/forsch/ForschLib/bin/Release/ForschLib.dll /home/forsch/ForschService/bin/Release/bin/ForschLib.dll
EXPOSE 80
WORKDIR /home/forsch/ForschService/bin/Release
ENTRYPOINT ["xsp4"]
CMD ["--port", "80"]
