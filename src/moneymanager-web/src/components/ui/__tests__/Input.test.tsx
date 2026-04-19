import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Input } from '../Input'

describe('Input', () => {
  it('renders an input element', () => {
    render(<Input placeholder="Enter text" />)
    expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument()
  })

  it('renders a label when provided', () => {
    render(<Input label="Username" />)
    expect(screen.getByLabelText('Username')).toBeInTheDocument()
  })

  it('calls onChange with the value on input', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<Input label="Name" onChange={onChange} />)
    await user.type(screen.getByLabelText('Name'), 'hello')
    expect(onChange).toHaveBeenCalledTimes(5)
    expect(onChange).toHaveBeenLastCalledWith('hello')
  })

  it('displays an error message', () => {
    render(<Input label="Email" error="Invalid email" />)
    expect(screen.getByText('Invalid email')).toBeInTheDocument()
  })

  it('applies error styling when error is present', () => {
    render(<Input label="Email" error="Bad" />)
    expect(screen.getByLabelText('Email').className).toContain('border-red-500')
  })

  it('is disabled when disabled prop is set', () => {
    render(<Input label="Locked" disabled />)
    expect(screen.getByLabelText('Locked')).toBeDisabled()
  })
})
