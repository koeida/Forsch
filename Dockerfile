FROM mono:6.12.0.107
RUN apt-get update && apt-get install --no-install-recommends -y mono-xsp4 
ADD . /home/forsch
RUN msbuild /home/forsch/Forcsh.sln
RUN mkdir -p /home/forsch/ForschService/bin/Debug/bin
RUN cp /home/forsch/ForschService/bin/Debug/*.dll /home/forsch/ForschService/bin/Debug/bin
EXPOSE 9000 
WORKDIR /home/forsch/ForschService/bin/Debug
ENTRYPOINT ["xsp4"]
CMD ["--port", "9000", "--nonstop"]
