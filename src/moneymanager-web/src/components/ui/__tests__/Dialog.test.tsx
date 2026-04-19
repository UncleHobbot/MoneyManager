import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Dialog } from '../Dialog'

describe('Dialog', () => {
  it('renders nothing when closed', () => {
    render(
      <Dialog open={false} onClose={vi.fn()}>
        <p>Content</p>
      </Dialog>,
    )
    expect(screen.queryByText('Content')).not.toBeInTheDocument()
  })

  it('renders children when open', () => {
    render(
      <Dialog open={true} onClose={vi.fn()}>
        <p>Hello dialog</p>
      </Dialog>,
    )
    expect(screen.getByText('Hello dialog')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(
      <Dialog open={true} onClose={vi.fn()} title="My Title">
        <p>Body</p>
      </Dialog>,
    )
    expect(screen.getByText('My Title')).toBeInTheDocument()
  })

  it('calls onClose when close button is clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Dialog open={true} onClose={onClose} title="Close me">
        <p>Body</p>
      </Dialog>,
    )
    await user.click(screen.getByLabelText('Close'))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when Escape is pressed', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Dialog open={true} onClose={onClose}>
        <p>Body</p>
      </Dialog>,
    )
    await user.keyboard('{Escape}')
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when backdrop is clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Dialog open={true} onClose={onClose}>
        <p>Body</p>
      </Dialog>,
    )
    await user.click(screen.getByText('Body').parentElement!.parentElement!.previousElementSibling as HTMLElement)
    expect(onClose).toHaveBeenCalledOnce()
  })
})
