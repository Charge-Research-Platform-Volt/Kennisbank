# Gotenberg with the fonts our documents use baked in, so Word previews match the original without
# font folders on the host. Only freely redistributable fonts (OFL/Apache), because this image is public.
FROM gotenberg/gotenberg:8-libreoffice

USER root

# Google Fonts that Debian packages
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        fonts-open-sans fonts-roboto fonts-roboto-slab fonts-lato fonts-montserrat \
        fonts-comfortaa fonts-inter fonts-adobe-sourcesans3 \
    && rm -rf /var/lib/apt/lists/*

# The rest of Google Docs' font menu, straight from google/fonts and pinned to a commit so builds are reproducible
ARG GOOGLE_FONTS_COMMIT=023eedd8780dd254e5905901473ed55e5475ee73
RUN dir=/usr/local/share/fonts/google && mkdir -p "$dir" \
    && for path in \
        "ofl/lora/Lora[wght].ttf" "ofl/lora/Lora-Italic[wght].ttf" \
        "ofl/merriweather/Merriweather[opsz,wdth,wght].ttf" "ofl/merriweather/Merriweather-Italic[opsz,wdth,wght].ttf" \
        "ofl/nunito/Nunito[wght].ttf" "ofl/nunito/Nunito-Italic[wght].ttf" \
        "ofl/oswald/Oswald[wght].ttf" \
        "ofl/playfairdisplay/PlayfairDisplay[wght].ttf" "ofl/playfairdisplay/PlayfairDisplay-Italic[wght].ttf" \
        "ofl/raleway/Raleway[wght].ttf" "ofl/raleway/Raleway-Italic[wght].ttf" \
        "ofl/robotomono/RobotoMono[wght].ttf" "ofl/robotomono/RobotoMono-Italic[wght].ttf" \
        "ofl/robotoserif/RobotoSerif[GRAD,opsz,wdth,wght].ttf" "ofl/robotoserif/RobotoSerif-Italic[GRAD,opsz,wdth,wght].ttf" \
        "ofl/ebgaramond/EBGaramond[wght].ttf" "ofl/ebgaramond/EBGaramond-Italic[wght].ttf" \
        "ofl/lobster/Lobster-Regular.ttf" \
        "ofl/caveat/Caveat[wght].ttf" \
        "ofl/lexend/Lexend[wght].ttf" \
        "ofl/pacifico/Pacifico-Regular.ttf" \
        "ofl/spectral/Spectral-Regular.ttf" "ofl/spectral/Spectral-Italic.ttf" "ofl/spectral/Spectral-Bold.ttf" "ofl/spectral/Spectral-BoldItalic.ttf" \
        "ofl/poppins/Poppins-Regular.ttf" "ofl/poppins/Poppins-Italic.ttf" "ofl/poppins/Poppins-Bold.ttf" "ofl/poppins/Poppins-BoldItalic.ttf" \
    ; do \
        curl -fsSL --globoff -o "$dir/$(basename "$path")" "https://raw.githubusercontent.com/google/fonts/${GOOGLE_FONTS_COMMIT}/$path" || exit 1; \
    done \
    && fc-cache -f

USER gotenberg

CMD ["gotenberg", "--libreoffice-restart-after=10", "--api-timeout=120s"]
