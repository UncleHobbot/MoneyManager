import type { FC } from 'react'
import { HandCoins, CreditCard, TrendingUp, CircleDollarSign } from 'lucide-react'
import type { LucideProps } from 'lucide-react'

const iconMap: Record<number, FC<LucideProps>> = {
  0: HandCoins,
  1: CreditCard,
  2: TrendingUp,
}

interface AccountIconProps {
  type?: number
  size?: number
  className?: string
}

export const AccountIcon: FC<AccountIconProps> = ({ type, size = 20, className = '' }) => {
  const Icon = (type != null && iconMap[type]) || CircleDollarSign
  return <Icon size={size} className={className} />
}
