import type { FC } from 'react'
import {
  Car,
  Lightbulb,
  Briefcase,
  GraduationCap,
  Tv,
  HandCoins,
  Landmark,
  Utensils,
  Gift,
  Stethoscope,
  Home,
  Banknote,
  TrendingUp,
  Baby,
  Handshake,
  Puzzle,
  Heart,
  Cat,
  ShoppingCart,
  FileText,
  ArrowLeftRight,
  Plane,
  Star,
} from 'lucide-react'
import type { LucideProps } from 'lucide-react'

const iconMap: Record<string, FC<LucideProps>> = {
  Auto: Car,
  Bills: Lightbulb,
  Business: Briefcase,
  Education: GraduationCap,
  Entertainment: Tv,
  Fees: HandCoins,
  Financial: Landmark,
  Food: Utensils,
  Gifts: Gift,
  Health: Stethoscope,
  Home: Home,
  Income: Banknote,
  Investment: TrendingUp,
  Kids: Baby,
  Loans: Handshake,
  Misc: Puzzle,
  Personal: Heart,
  Pets: Cat,
  Shopping: ShoppingCart,
  Taxes: FileText,
  Transfer: ArrowLeftRight,
  Travel: Plane,
}

interface CategoryIconProps {
  icon?: string
  size?: number
  className?: string
}

export const CategoryIcon: FC<CategoryIconProps> = ({ icon, size = 20, className = '' }) => {
  const Icon = (icon && iconMap[icon]) || Star
  return <Icon size={size} className={className} />
}
