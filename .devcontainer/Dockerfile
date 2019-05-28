FROM ubuntu:16.04

COPY settings.vscode.json /root/.vscode-remote/data/Machine/settings.json

ENV DEBIAN_FRONTEND=noninteractive
ENV PATH "$PATH:/root/.cargo/bin:/root/.dotnet"

# TODO: Audit this list for dependencies we don't actually need
RUN apt-get update && apt-get -y install --no-install-recommends \
    git \
    procps \
    lsb-release \
    apt-utils \
    ca-certificates \
    libc6-dev \
    libcurl4-openssl-dev \
    libgcc1 \
    gcc \
    lldb-3.9 \
    python-lldb-3.9 \
    clang-3.9 \
    libkrb5-dev \
    libicu55 \
    liblttng-ust0 \
    libssl1.0.0 \
    libstdc++6 \
    libunwind8 \
    libuuid1 \
    zlib1g-dev \
    apt-transport-https \
    curl \
    2>&1

RUN curl -sL https://deb.nodesource.com/setup_12.x | bash -
RUN apt-get -y install --no-install-recommends nodejs
RUN npm install -g @angular/cli

RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --default-toolchain nightly-2019-05-22
RUN rustup component add rls rust-analysis rust-src

RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Version 3.0.100-preview5-011568

RUN apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*
ENV DEBIAN_FRONTEND=dialog

ENV SHELL /bin/bash
