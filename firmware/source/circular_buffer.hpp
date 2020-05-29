#ifndef PA_CIRCULAR_BUFFER_HPP
#define PA_CIRCULAR_BUFFER_HPP

#include <algorithm>
#include <cstddef>
#include <iterator>
#include <stdexcept>

namespace pa {
    namespace circular_buffer_iterators {
	template<class Traits> struct nonconst_traits;

	template<class Traits> struct const_traits {
	    typedef typename Traits::const_pointer pointer;
	    typedef typename Traits::const_reference reference;

	    typedef nonconst_traits<Traits> nonconst_self;
	};

	template<class Traits> struct nonconst_traits {
	    typedef typename Traits::pointer pointer;
	    typedef typename Traits::reference reference;

	    typedef nonconst_traits<Traits> nonconst_self;
	};

	template<class Traits, class CircularBuffer> class iterator {
	public:
	    typedef std::random_access_iterator_tag iterator_category;
	    typedef typename CircularBuffer::difference_type difference_type;
	    typedef typename CircularBuffer::size_type size_type;
	    typedef typename CircularBuffer::value_type value_type;
	    typedef typename Traits::pointer pointer;
	    typedef typename Traits::reference reference;

	    typedef iterator<typename Traits::nonconst_self, CircularBuffer> nonconst_self;

	    iterator() : cb_(0), p_(0) { }

	    iterator(const nonconst_self& it) : cb_(it.cb_), p_(it.p_), parity_(it.parity_) { }

	    iterator(const CircularBuffer* cb, pointer p, bool parity) : cb_(cb), p_(p), parity_(parity) { }

	    reference operator*() const {
		return *p_;
	    }

	    pointer operator->() const {
		return p_;
	    }

	    template <class Traits0>
	    difference_type operator-(const iterator<Traits0, CircularBuffer>& it) const {
		return offset() - it.offset();
	    }

	    iterator& operator++() {
		increment();
		return *this;
	    }

	    iterator operator++(int) {
		iterator<Traits, CircularBuffer> tmp = *this;
		increment();
		return tmp;
	    }

	    iterator& operator--() {
		decrement();
		return *this;
	    }

	    iterator operator--(int) {
		iterator<Traits, CircularBuffer> tmp = *this;
		decrement();
		return tmp;
	    }

	    iterator operator+=(difference_type n) {
		if (n > 0) {
		    p_ += n;
		    if (p_ >= cb_->storage_ + cb_->max_size()) {
			p_ -= cb_->max_size();
			parity_ = !parity_;
		    }
		} else {
		    p_ -= -n;
		}

		return *this;
	    }

	    iterator operator+(difference_type n) const {
		return iterator<Traits, CircularBuffer>(*this) += n;
	    }

	    iterator operator-=(difference_type n) {
		if (n > 0) {
		    p_ -= n;
		    if (p_ < cb_->storage_) {
			p_ += cb_->max_size();
			parity_ = !parity_;
		    }
		} else {
		    p_ += -n;
		}

		return *this;
	    }

	    iterator operator-(difference_type n) const {
		return iterator<Traits, CircularBuffer>(*this) -= n;
	    }

	    reference operator[](difference_type n) const { return *(*this + n); }

	    bool operator==(const iterator<Traits, CircularBuffer>& other) const {
		return p_ == other.p_ && parity_ == other.parity_;
	    }

	    bool operator!=(const iterator<Traits, CircularBuffer>& other) const {
		return p_ != other.p_ || parity_ != other.parity_;
	    }
	    
	    void increment() {
		if (++p_ == cb_->storage_ + cb_->max_size()) {
		    p_ = const_cast<pointer>(cb_->storage_);
		    parity_ = !parity_;
		}
	    }

	    void decrement() {
		if (p_ == cb_->storage_) {
		    p_ = const_cast<pointer>(cb_->storage_) + cb_->max_size();
		    parity_ = !parity_;
		}
		--p_;
	    }

	    difference_type offset() const {
		if (p_ == cb_->read_) {
		    return (parity_ == cb_->read_parity_) ? 0 : cb_->max_size();
		} else if (p_ > cb_->read_) {
		    return p_ - cb_->read_;
		} else {
		    return p_ + cb_->max_size() - cb_->read_;
		}
	    }

	    const CircularBuffer* cb_;
	    pointer p_;
	    bool parity_;
	};
    }

    template<typename T, std::size_t MAX_SIZE> class circular_buffer {
    public:
	typedef T value_type;
	typedef T& reference;
	typedef const T& const_reference;
	typedef T* pointer;
	typedef const T* const_pointer;
	typedef std::ptrdiff_t difference_type;
	typedef std::size_t size_type;
	typedef circular_buffer_iterators::iterator<circular_buffer_iterators::nonconst_traits<circular_buffer<T, MAX_SIZE> >, circular_buffer<T, MAX_SIZE> > iterator;
	typedef circular_buffer_iterators::iterator<circular_buffer_iterators::const_traits<circular_buffer<T, MAX_SIZE> >, circular_buffer<T, MAX_SIZE> > const_iterator;
	typedef std::reverse_iterator<circular_buffer_iterators::iterator<circular_buffer_iterators::nonconst_traits<circular_buffer<T, MAX_SIZE> >, circular_buffer<T, MAX_SIZE> > > reverse_iterator;
	typedef std::reverse_iterator<circular_buffer_iterators::iterator<circular_buffer_iterators::const_traits<circular_buffer<T, MAX_SIZE> >, circular_buffer<T, MAX_SIZE> > > const_reverse_iterator;

	typedef std::pair<pointer, size_type> array_range;
	typedef std::pair<const_pointer, size_type> const_array_range;

	explicit circular_buffer() {
	    clear();
	}

	circular_buffer(const circular_buffer<T, MAX_SIZE>& cb) {
	    *this = cb;
	}

	template<class InputIterator>
	circular_buffer(InputIterator first, InputIterator last) {
	    assign(first, last);
	}

	iterator begin() { return iterator(this, read_, read_parity_); }
	iterator end() { return iterator(this, write_, write_parity_); }

	const_iterator begin() const { return const_iterator(this, read_, read_parity_); }
	const_iterator end() const { return const_iterator(this, write_, write_parity_); }

	reverse_iterator rbegin() { return std::reverse_iterator<iterator>(begin()); }
	reverse_iterator rend() { return std::reverse_iterator<iterator>(end()); }

	const_reverse_iterator rbegin() const { return std::reverse_iterator<const_iterator>(begin()); }
	const_reverse_iterator rend() const { return std::reverse_iterator<const_iterator>(end()); }

	reference operator[](size_type index) {
	    pointer p = read_ + index;
	    if (p > storage_ + MAX_SIZE) {
		return *(p - MAX_SIZE);
	    } else {
		return *p;
	    }
	}

	const_reference operator[](size_type index) const {
	    pointer p = read_ + index;
	    if (p > storage_ + MAX_SIZE) {
		return *(p - MAX_SIZE);
	    } else {
		return *p;
	    }
	}

	reference at(size_type index) {
	    if (index >= size()) {
		throw std::out_of_range("circular_buffer::at");
	    }
	    return operator[](index);
	}

	const_reference at(size_type index) const {
	    if (index >= size()) {
		throw std::out_of_range("circular_buffer::at");
	    }
	    return operator[](index);
	}

	reference front() { return *read_; }
	reference back() { 
	    return (write_ > storage_) ? *(write_ - 1) : storage_[MAX_SIZE - 1];
	}
	
	const_reference front() const { return *read_; }
	const_reference back() const { 
	    return (write_ > storage_) ? *(write_ - 1) : storage_[MAX_SIZE - 1];
	}

	array_range array_one() {
	    return array_range(read_, (read_ < write_ || empty()) ? write_ - read_ : storage_ + MAX_SIZE - read_);
	}

	array_range array_two() {
	    return array_range(storage_, (read_ > write_ || full()) ? write_ - storage_ : 0);
	}

	const_array_range array_one() const {
	    return const_array_range(read_, (read_parity_ == write_parity_) ? write_ - read_ : storage_ + MAX_SIZE - read_);
	}

	const_array_range array_two() const {
	    return const_array_range(storage_, (read_parity_ == write_parity_) ? 0 : write_ - storage_);
	}

	pointer linearize() {
	    if (!is_linearized()) {
		size_t n = size();
		size_t steps = storage_ + MAX_SIZE - read_;
		std::reverse(storage_, storage_ + MAX_SIZE);
		std::reverse(storage_, storage_ + steps);
		std::reverse(storage_ + steps, storage_ + MAX_SIZE);

		read_ = storage_;
		if (n == max_size()) {
		    write_ = storage_;
		    read_parity_ = 0;
		    write_parity_ = 1;
		} else {
		    write_ = storage_ + n;
		    read_parity_ = write_parity_ = 0;
		}
	    }

	    return read_;
	}

	void rotate(const_iterator new_begin) {
	    if (full()) {
		read_ = write_ = const_cast<pointer>(new_begin.p_);
	    } else {
		difference_type n = new_begin - begin();
		difference_type m = end() - new_begin;
		if (m > n) {
		    // rotate left by n
		    for (int i = 0; i < n; ++i) {
			push_back(front());
			pop_front();
		    }
		} else {
		    // rotate right by m
		    for (int i = 0; i < m; ++i) {
			push_front(back());
			pop_back();
		    }
		}
	    }
	}

	bool is_linearized() const {
	    return (read_parity_ == write_parity_);
	}

	circular_buffer<T, MAX_SIZE>& operator=(const circular_buffer<T, MAX_SIZE>& cb) {
	    assign(cb.begin(), cb.end());
	    return *this;
	}

	template<class InputIterator>
	void assign(InputIterator first, InputIterator last) {
	    clear();

	    for (; first != last; ++first) {
		push_back(*first);
	    }
	}

	void swap(circular_buffer<T, MAX_SIZE>& other) {
	    for (size_type i = 0; i < MAX_SIZE; ++i) {
		std::swap(storage_[i], other.storage_[i]);
	    }
	    std::swap(read_, other.read_);
	    std::swap(write_, other.write_);
	    std::swap(read_parity_, other.read_parity_);
	    std::swap(write_parity_, other.write_parity_);
	}

	void push_back(const_reference item = value_type()) {
	    if (full()) {
		increment(read_, read_parity_);
	    }
	    *write_ = item;
	    increment(write_, write_parity_);
	}

	void pop_back() {
	    decrement(write_, write_parity_);
	}

	void push_front(const_reference item = value_type()) {
	    if (full()) {
		decrement(write_, write_parity_);
	    }
	    decrement(read_, read_parity_);
	    *read_ = item;
	}
	
	void pop_front() {
	    increment(read_, read_parity_);
	}

	iterator insert(iterator pos, const_reference item = value_type()) {
	    if (full()) {
		if (pos == begin()) {
		    return begin();
		} else {
		    increment(read_, read_parity_);
		}
	    }
	    
	    increment(write_, write_parity_);
	    
	    // move everything above pos up 1
	    for (iterator it = end() - 1; it != pos; --it) {
		*it = *(it - 1);
	    }
	    *pos = item;
	    return pos;
	}

	template<class InputIterator>
	void insert(iterator pos, InputIterator first, InputIterator last) {
	    while (first != last) {
		pos = insert(pos, *first) + 1;
		++first;
	    }
	}

	iterator rinsert(iterator pos, const_reference item = value_type()) {
	    if (full()) {
		if (pos == end()) {
		    return end();
		} else {
		    decrement(write_, write_parity_);
		}
	    }

	    decrement(read_, read_parity_);
	    pos = pos - 1;
	    // move everything below pos down 1

	    for (iterator it = begin(); it != pos; ++it) {
		*it = *(it + 1);
	    }
	    
	    *pos = item;
	    return pos;
	}

	template<class InputIterator>
	void rinsert(iterator pos, InputIterator first, InputIterator last) {
	    while (first != last) {
		pos = rinsert(pos, *first) + 1;
		++first;
	    }
	}

	iterator erase(iterator pos) {
	    for (iterator it = pos; it != end(); ++it) {
		*it = *(it + 1);
	    }
	    pop_back();
	    return pos;
	}

	iterator erase(iterator first, iterator last) {
	    difference_type range = last - first;

	    for (; last != end(); ++first, ++last) {
		*first = *last;
	    }

	    while (range--) {
		pop_back();
	    }

	    return first;
	}

	iterator rerase(iterator pos) {
	    for (iterator it = pos; it != begin(); --it) {
		*it = *(it - 1);
	    }
	    pop_front();
	    return pos;
	}

	iterator rerase(iterator first, iterator last) {
	    difference_type range = last - first;
	    for (; first != begin(); --first, --last) {
		*(last - 1) = *(first - 1);
	    }

	    while (range--) {
		pop_front();
	    }

	    return last;
	}

	void erase_begin(size_type n) {
	    read_ += n;
	    if (read_ > storage_ + MAX_SIZE) {
		read_ -= MAX_SIZE;
		read_parity_ = !read_parity_;
	    }
	}

	void erase_end(size_type n) {
	    write_ -= n;
	    if (write_ < storage_) {
		write_ += MAX_SIZE;
		write_parity_ = !write_parity_;
	    }
	}

	size_type size() const {
	    if (read_parity_ == write_parity_) {
		return write_ - read_;
	    } else {
		return write_ + MAX_SIZE - read_;
	    }
	}

	size_type max_size() const { return MAX_SIZE; }
	bool empty() const { return (read_ == write_ && read_parity_ == write_parity_); }
	bool full() const { return (read_ == write_ && read_parity_ != write_parity_); }

	void clear() {
	    read_ = storage_;
	    write_ = storage_;
	    read_parity_ = 0;
	    write_parity_ = 0;
	}

    private:
	void decrement(pointer& p, bool& parity) {
	    if (p == storage_) {
		p = storage_ + MAX_SIZE;
		parity = !parity;
	    }
	    --p;
	}

	void increment(pointer& p, bool& parity) {
	    if (++p == storage_ + MAX_SIZE) {
		p = storage_;
		parity = !parity;
	    }
	}

	pointer read_;
	pointer write_;

	bool read_parity_;
	bool write_parity_;

	value_type storage_[MAX_SIZE];

	friend iterator;
	friend const_iterator;
    };

    template<class T, std::size_t MAX_SIZE>
    bool operator==(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return lhs.size() == rhs.size() && std::equal(lhs.begin(), lhs.end(), rhs.begin());
    }

    template<class T, std::size_t MAX_SIZE>
    bool operator!=(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return !(lhs == rhs);
    }

    template<class T, std::size_t MAX_SIZE>
    bool operator<(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return std::lexicographical_compare(lhs.begin(), lhs.end(), rhs.begin(), rhs.end());
    }

    template<class T, std::size_t MAX_SIZE>
    bool operator>(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return rhs < lhs;
    }

    template<class T, std::size_t MAX_SIZE>
    bool operator<=(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return !(rhs < lhs);
    }

    template<class T, std::size_t MAX_SIZE>
    bool operator>=(const circular_buffer<T, MAX_SIZE>& lhs, const circular_buffer<T, MAX_SIZE>& rhs) {
	return !(lhs < rhs);
    }

    template<class T, std::size_t MAX_SIZE>
    void swap(circular_buffer<T, MAX_SIZE>& lhs, circular_buffer<T, MAX_SIZE>& rhs) {
	lhs.swap(rhs);
    }
}

#endif
