FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS base

ENV OPENCV_VERSION=4.5.0

WORKDIR /

RUN apt-get update && apt-get -y install --no-install-recommends \
    apt-transport-https \
    software-properties-common \
    wget \
    unzip \
    curl \
    ca-certificates
    #bzip2 \
    #grep sed dpkg 

# Install opencv dependencies
RUN apt-get update && apt-get -y install --no-install-recommends \
      build-essential \
      cmake \
      git \
      gfortran \
      libjpeg8-dev \
      libpng-dev \
      software-properties-common
RUN add-apt-repository "deb http://security.ubuntu.com/ubuntu xenial-security main" && apt-get update && apt-get -y install --no-install-recommends \
      libjasper1 \
      libtiff-dev \
      libavcodec-dev \
      libavformat-dev \
      libswscale-dev \
      libdc1394-22-dev \
      libxine2-dev \
      libv4l-dev

RUN cd /usr/include/linux && \
    ln -s -f ../libv4l1-videodev.h videodev.h && \
    cd ~ 
RUN apt-get update && apt-get -y install --no-install-recommends \
      libgtk2.0-dev \
      libtbb-dev \
      qt5-default \
      libatlas-base-dev \
      libfaac-dev \
      libmp3lame-dev \
      libtheora-dev \
      libvorbis-dev \
      libxvidcore-dev \
      libopencore-amrnb-dev \
      libopencore-amrwb-dev \
      libavresample-dev \
      x264 \
      v4l-utils \
      libwebp-dev 
RUN apt-get update && apt-get -y install --no-install-recommends \
      tesseract-ocr libtesseract-dev libleptonica-dev \
      libgdiplus \
    && apt-get -y clean \
    && rm -rf /var/lib/apt/lists/*

# Setup opencv and opencv-contrib source
RUN wget https://github.com/opencv/opencv/archive/${OPENCV_VERSION}.zip && \
    unzip ${OPENCV_VERSION}.zip && \
    rm ${OPENCV_VERSION}.zip && \
    mv opencv-${OPENCV_VERSION} opencv && \
    wget https://github.com/opencv/opencv_contrib/archive/${OPENCV_VERSION}.zip && \
    unzip ${OPENCV_VERSION}.zip && \
    rm ${OPENCV_VERSION}.zip && \
    mv opencv_contrib-${OPENCV_VERSION} opencv_contrib

# Build OpenCV
RUN cd opencv && mkdir build && cd build && \
    cmake \
    -D OPENCV_EXTRA_MODULES_PATH=/opencv_contrib/modules \
    -D CMAKE_BUILD_TYPE=RELEASE \
    -D BUILD_SHARED_LIBS=OFF \
    -D ENABLE_CXX11=ON \
    -D BUILD_EXAMPLES=OFF \
    -D BUILD_DOCS=OFF \
    -D BUILD_PERF_TESTS=OFF \
    -D BUILD_TESTS=OFF \
    -D BUILD_JAVA=OFF \
    -D BUILD_opencv_app=OFF \
    -D BUILD_opencv_java_bindings_generator=OFF \
    -D BUILD_opencv_python_bindings_generator=OFF \
    -D BUILD_opencv_python_tests=OFF \
    -D BUILD_opencv_ts=OFF \
    -D BUILD_opencv_js=OFF \ 
    -D BUILD_opencv_bioinspired=OFF \
    -D BUILD_opencv_ccalib=OFF \
    -D BUILD_opencv_datasets=OFF \
    -D BUILD_opencv_dnn_objdetect=OFF \
    -D BUILD_opencv_dnn_superres=OFF \
    -D BUILD_opencv_dpm=OFF \
    -D BUILD_opencv_fuzzy=OFF \
    -D BUILD_opencv_gapi=OFF \
    -D BUILD_opencv_intensity_transform=OFF \
    -D BUILD_opencv_mcc=OFF \
    -D BUILD_opencv_rapid=OFF \
    -D BUILD_opencv_reg=OFF \
    -D BUILD_opencv_stereo=OFF \
    -D BUILD_opencv_structured_light=OFF \
    -D BUILD_opencv_surface_matching=OFF \
    -D BUILD_opencv_videostab=OFF \
    -D WITH_GSTREAMER=OFF \ 
    -D OPENCV_ENABLE_NONFREE=ON \
    .. && make -j6 && make install && ldconfig

# Download OpenCvSharp
RUN git clone https://github.com/shimat/opencvsharp.git && cd opencvsharp

# Install the Extern lib.
RUN mkdir /opencvsharp/make && cd /opencvsharp/make && \
    cmake -D CMAKE_INSTALL_PREFIX=/opencvsharp/make /opencvsharp/src && \
    make -j && make install && \
    rm -rf /opencv && \
    rm -rf /opencv_contrib && \
    mkdir /artifacts && \
    cp /opencvsharp/make/OpenCvSharpExtern/libOpenCvSharpExtern.so /artifacts/

WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY . .

RUN dotnet restore "grpcExtension.csproj"
RUN dotnet build "grpcExtension.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "grpcExtension.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Required for using System.Drawing.Common
RUN apt-get update -y && \
    apt-get install -y --no-install-recommends libgdiplus

# Debug
RUN apt-get update \
 && apt-get install -y --no-install-recommends unzip procps \
 && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

RUN cp /opencvsharp/make/OpenCvSharpExtern/libOpenCvSharpExtern.so /usr/local/lib
RUN cp /opencvsharp/make/OpenCvSharpExtern/libOpenCvSharpExtern.so .
RUN cd /usr/lib
RUN ln -s libgdiplus.so gdiplus.dll

RUN export LD_LIBRARY_PATH="/lib:/usr/lib:/usr/local/lib"

ENTRYPOINT ["dotnet", "GrpcExtension.dll"]