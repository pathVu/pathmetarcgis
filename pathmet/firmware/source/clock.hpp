#pragma once

#include <chrono>

struct clock {
    using rep = std::int64_t;
    using period = std::micro;
    using duration = std::chrono::duration<rep, period>;
    using time_point = std::chrono::time_point<clock>;
    static constexpr bool is_steady = true;

    static time_point now() noexcept;

    static void sleep_for(duration duration) {
        time_point until = now() + duration;
        while (now() < until) { }
    }
};
