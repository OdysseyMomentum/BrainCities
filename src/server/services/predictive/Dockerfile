# ---- Base python ----
FROM tiangolo/uvicorn-gunicorn:python3.8 AS base
RUN pip install --upgrade pip
# Create app directory
WORKDIR /app

# ---- Dependencies ----
FROM base AS dependencies  
COPY ./services/predictive/requirements.txt ./
# install app dependencies
RUN pip install -r requirements.txt

# ---- Copy Files/Build ----
FROM dependencies AS build  
WORKDIR /app
COPY ./services/predictive /app
# Build / Compile if required
RUN chmod +x /app/*.sh

# --- Release with Alpine ----
FROM tiangolo/uvicorn-gunicorn:python3.8 AS release  
LABEL maintainer="Joris Navarro <joris.navarro@braincities.co>"
RUN pip install --upgrade pip
# Create app directory
WORKDIR /app

COPY --from=dependencies /app/requirements.txt ./
COPY --from=dependencies /root/.cache /root/.cache

# Install app dependencies
RUN pip install --no-cache-dir fastapi
RUN pip install -r requirements.txt
COPY --from=build /app ./
RUN pwd

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80", "--root-path", "predictive-api"]
