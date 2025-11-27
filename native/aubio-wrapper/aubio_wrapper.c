/**
 * aubio_wrapper.c - Thin wrapper DLL for aubio static library
 *
 * This wrapper re-exports the aubio functions needed for beat detection
 * so they can be called via P/Invoke from .NET.
 */

#include <aubio/aubio.h>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

/* =============================================================================
 * fvec_t functions
 * ============================================================================= */

EXPORT fvec_t* wrapper_new_fvec(unsigned int length) {
    return new_fvec(length);
}

EXPORT void wrapper_del_fvec(fvec_t* s) {
    del_fvec(s);
}

EXPORT float wrapper_fvec_get_sample(const fvec_t* s, unsigned int position) {
    return fvec_get_sample(s, position);
}

EXPORT void wrapper_fvec_set_sample(fvec_t* s, float data, unsigned int position) {
    fvec_set_sample(s, data, position);
}

EXPORT float* wrapper_fvec_get_data(const fvec_t* s) {
    return fvec_get_data(s);
}

EXPORT void wrapper_fvec_zeros(fvec_t* s) {
    fvec_zeros(s);
}

EXPORT unsigned int wrapper_fvec_get_length(const fvec_t* s) {
    return s->length;
}

/* =============================================================================
 * aubio_tempo_t functions (beat tracking)
 * ============================================================================= */

EXPORT aubio_tempo_t* wrapper_new_aubio_tempo(const char* method,
    unsigned int buf_size, unsigned int hop_size, unsigned int samplerate) {
    return new_aubio_tempo(method, buf_size, hop_size, samplerate);
}

EXPORT void wrapper_aubio_tempo_do(aubio_tempo_t* o, const fvec_t* input, fvec_t* tempo) {
    aubio_tempo_do(o, input, tempo);
}

EXPORT unsigned int wrapper_aubio_tempo_get_last(aubio_tempo_t* o) {
    return aubio_tempo_get_last(o);
}

EXPORT float wrapper_aubio_tempo_get_last_s(aubio_tempo_t* o) {
    return aubio_tempo_get_last_s(o);
}

EXPORT float wrapper_aubio_tempo_get_last_ms(aubio_tempo_t* o) {
    return aubio_tempo_get_last_ms(o);
}

EXPORT unsigned int wrapper_aubio_tempo_set_silence(aubio_tempo_t* o, float silence) {
    return aubio_tempo_set_silence(o, silence);
}

EXPORT float wrapper_aubio_tempo_get_silence(aubio_tempo_t* o) {
    return aubio_tempo_get_silence(o);
}

EXPORT unsigned int wrapper_aubio_tempo_set_threshold(aubio_tempo_t* o, float threshold) {
    return aubio_tempo_set_threshold(o, threshold);
}

EXPORT float wrapper_aubio_tempo_get_threshold(aubio_tempo_t* o) {
    return aubio_tempo_get_threshold(o);
}

EXPORT float wrapper_aubio_tempo_get_period(aubio_tempo_t* o) {
    return aubio_tempo_get_period(o);
}

EXPORT float wrapper_aubio_tempo_get_period_s(aubio_tempo_t* o) {
    return aubio_tempo_get_period_s(o);
}

EXPORT float wrapper_aubio_tempo_get_bpm(aubio_tempo_t* o) {
    return aubio_tempo_get_bpm(o);
}

EXPORT float wrapper_aubio_tempo_get_confidence(aubio_tempo_t* o) {
    return aubio_tempo_get_confidence(o);
}

EXPORT void wrapper_del_aubio_tempo(aubio_tempo_t* o) {
    del_aubio_tempo(o);
}

/* =============================================================================
 * aubio_onset_t functions (onset detection)
 * ============================================================================= */

EXPORT aubio_onset_t* wrapper_new_aubio_onset(const char* method,
    unsigned int buf_size, unsigned int hop_size, unsigned int samplerate) {
    return new_aubio_onset(method, buf_size, hop_size, samplerate);
}

EXPORT void wrapper_aubio_onset_do(aubio_onset_t* o, const fvec_t* input, fvec_t* onset) {
    aubio_onset_do(o, input, onset);
}

EXPORT unsigned int wrapper_aubio_onset_get_last(const aubio_onset_t* o) {
    return aubio_onset_get_last(o);
}

EXPORT float wrapper_aubio_onset_get_last_s(const aubio_onset_t* o) {
    return aubio_onset_get_last_s(o);
}

EXPORT float wrapper_aubio_onset_get_last_ms(const aubio_onset_t* o) {
    return aubio_onset_get_last_ms(o);
}

EXPORT unsigned int wrapper_aubio_onset_set_silence(aubio_onset_t* o, float silence) {
    return aubio_onset_set_silence(o, silence);
}

EXPORT float wrapper_aubio_onset_get_silence(const aubio_onset_t* o) {
    return aubio_onset_get_silence(o);
}

EXPORT float wrapper_aubio_onset_get_descriptor(const aubio_onset_t* o) {
    return aubio_onset_get_descriptor(o);
}

EXPORT float wrapper_aubio_onset_get_thresholded_descriptor(const aubio_onset_t* o) {
    return aubio_onset_get_thresholded_descriptor(o);
}

EXPORT unsigned int wrapper_aubio_onset_set_threshold(aubio_onset_t* o, float threshold) {
    return aubio_onset_set_threshold(o, threshold);
}

EXPORT float wrapper_aubio_onset_get_threshold(const aubio_onset_t* o) {
    return aubio_onset_get_threshold(o);
}

EXPORT unsigned int wrapper_aubio_onset_set_minioi_ms(aubio_onset_t* o, float minioi) {
    return aubio_onset_set_minioi_ms(o, minioi);
}

EXPORT float wrapper_aubio_onset_get_minioi_ms(const aubio_onset_t* o) {
    return aubio_onset_get_minioi_ms(o);
}

EXPORT void wrapper_aubio_onset_reset(aubio_onset_t* o) {
    aubio_onset_reset(o);
}

EXPORT void wrapper_del_aubio_onset(aubio_onset_t* o) {
    del_aubio_onset(o);
}

/* =============================================================================
 * Convenience functions for .NET interop
 * ============================================================================= */

/**
 * Creates a tempo tracker with sensible defaults for real-time audio.
 * Returns NULL on failure.
 */
EXPORT aubio_tempo_t* wrapper_create_tempo_tracker(unsigned int samplerate) {
    // Use "default" method, 1024 buffer, 512 hop (good for real-time)
    return new_aubio_tempo("default", 1024, 512, samplerate);
}

/**
 * Process a buffer of samples and return 1 if a beat was detected, 0 otherwise.
 * Also outputs the current BPM via the bpm pointer.
 */
EXPORT int wrapper_process_tempo(aubio_tempo_t* tempo, const float* samples,
    unsigned int num_samples, float* out_bpm) {
    if (!tempo || !samples || num_samples == 0) {
        return 0;
    }

    // Create temporary vectors
    fvec_t* input = new_fvec(num_samples);
    fvec_t* output = new_fvec(1);

    if (!input || !output) {
        if (input) del_fvec(input);
        if (output) del_fvec(output);
        return 0;
    }

    // Copy samples to input vector
    for (unsigned int i = 0; i < num_samples; i++) {
        fvec_set_sample(input, samples[i], i);
    }

    // Process
    aubio_tempo_do(tempo, input, output);

    // Check if beat detected
    int beat_detected = fvec_get_sample(output, 0) > 0.0f ? 1 : 0;

    // Get BPM
    if (out_bpm) {
        *out_bpm = aubio_tempo_get_bpm(tempo);
    }

    // Cleanup
    del_fvec(input);
    del_fvec(output);

    return beat_detected;
}

/**
 * Creates an onset detector with sensible defaults for real-time audio.
 * Returns NULL on failure.
 * method can be: "energy", "hfc", "complex", "phase", "wphase", "specdiff",
 *                "kl", "mkl", "specflux", "default"
 */
EXPORT aubio_onset_t* wrapper_create_onset_detector(const char* method, unsigned int samplerate) {
    if (!method) method = "default";
    // Use 1024 buffer, 512 hop
    return new_aubio_onset(method, 1024, 512, samplerate);
}

/**
 * Process a buffer of samples and return 1 if an onset was detected, 0 otherwise.
 */
EXPORT int wrapper_process_onset(aubio_onset_t* onset, const float* samples,
    unsigned int num_samples) {
    if (!onset || !samples || num_samples == 0) {
        return 0;
    }

    // Create temporary vectors
    fvec_t* input = new_fvec(num_samples);
    fvec_t* output = new_fvec(1);

    if (!input || !output) {
        if (input) del_fvec(input);
        if (output) del_fvec(output);
        return 0;
    }

    // Copy samples to input vector
    for (unsigned int i = 0; i < num_samples; i++) {
        fvec_set_sample(input, samples[i], i);
    }

    // Process
    aubio_onset_do(onset, input, output);

    // Check if onset detected
    int onset_detected = fvec_get_sample(output, 0) > 0.0f ? 1 : 0;

    // Cleanup
    del_fvec(input);
    del_fvec(output);

    return onset_detected;
}
