FROM mono:6.12.0.107
ADD . /home/forsch
RUN apt-get update && apt-get install --no-install-recommends -y mono-xsp4 && msbuild /home/forsch/Forcsh.sln
WORKDIR /home/forsch/ForschService/bin/Release
RUN mkdir /home/forsch/ForschService/bin/Release/bin
RUN cp /home/forsch/ForschLib/bin/Release/ForschLib.dll /home/forsch/ForschService/bin/Release/bin/ForschLib.dll
