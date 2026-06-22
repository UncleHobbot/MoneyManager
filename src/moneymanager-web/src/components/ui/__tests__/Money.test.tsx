import { render, screen } from '@testing-library/react'
import { Money } from '../Money'

describe('Money', () => {
  describe('basic rendering', () => {
    it('renders formatted value with default options', () => {
      render(<Money amount={45.67} />)
      expect(screen.getByText('$45.67')).toBeInTheDocument()
    })

    it('renders absolute value for negative by default', () => {
      render(<Money amount={-45.67} />)
      expect(screen.getByText('$45.67')).toBeInTheDocument()
    })
  })

  describe('signed', () => {
    it('prefixes + for positive when signed', () => {
      render(<Money amount={45.67} signed />)
      expect(screen.getByText('+$45.67')).toBeInTheDocument()
    })

    it('prefixes - for negative when signed', () => {
      render(<Money amount={-45.67} signed />)
      expect(screen.getByText('-$45.67')).toBeInTheDocument()
    })
  })

  describe('color', () => {
    it('applies green class for positive when colored', () => {
      render(<Money amount={100} color />)
      expect(screen.getByText('$100.00')).toHaveClass('text-green-600')
    })

    it('applies red class for negative when colored', () => {
      render(<Money amount={-100} color />)
      expect(screen.getByText('$100.00')).toHaveClass('text-red-600')
    })

    it('does not apply color class by default', () => {
      render(<Money amount={100} />)
      const span = screen.getByText('$100.00')
      expect(span).not.toHaveClass('text-green-600')
      expect(span).not.toHaveClass('text-red-600')
    })

    it('treats zero as non-negative (green) when colored', () => {
      render(<Money amount={0} color />)
      expect(screen.getByText('$0.00')).toHaveClass('text-green-600')
    })
  })

  describe('fractionDigits', () => {
    it('respects fractionDigits={0}', () => {
      render(<Money amount={45.67} fractionDigits={0} />)
      expect(screen.getByText('$46')).toBeInTheDocument()
    })
  })

  describe('className', () => {
    it('merges custom className', () => {
      render(<Money amount={45.67} className="font-bold" />)
      expect(screen.getByText('$45.67')).toHaveClass('font-bold')
    })

    it('merges className with color class', () => {
      render(<Money amount={-50} color className="font-bold" />)
      const span = screen.getByText('$50.00')
      expect(span).toHaveClass('text-red-600')
      expect(span).toHaveClass('font-bold')
    })
  })
})
