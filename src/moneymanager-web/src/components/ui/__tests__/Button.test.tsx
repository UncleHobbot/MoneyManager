import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '../Button'

describe('Button', () => {
  it('renders children text', () => {
    render(<Button>Click me</Button>)
    expect(screen.getByRole('button', { name: /click me/i })).toBeInTheDocument()
  })

  it('calls onClick when clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<Button onClick={onClick}>Press</Button>)
    await user.click(screen.getByRole('button'))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('is disabled when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('is disabled when loading', () => {
    render(<Button loading>Loading</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('does not fire onClick when disabled', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<Button disabled onClick={onClick}>No</Button>)
    await user.click(screen.getByRole('button'))
    expect(onClick).not.toHaveBeenCalled()
  })

  it('applies variant classes', () => {
    const { rerender } = render(<Button variant="danger">Del</Button>)
    expect(screen.getByRole('button').className).toContain('bg-red-600')

    rerender(<Button variant="secondary">Sec</Button>)
    expect(screen.getByRole('button').className).toContain('bg-gray-100')
  })

  it('applies size classes', () => {
    render(<Button size="lg">Big</Button>)
    expect(screen.getByRole('button').className).toContain('px-6')
  })

  it('renders icon alongside children', () => {
    render(<Button icon={<span data-testid="icon">★</span>}>Star</Button>)
    expect(screen.getByTestId('icon')).toBeInTheDocument()
    expect(screen.getByText('Star')).toBeInTheDocument()
  })
})
