import { ReactNode } from 'react';
import { requireRoleWithRedirect, requireAnyRoleWithRedirect, requireRole, requireAnyRole } from '@/lib/auth-server';

interface PageRoleGuardProps
{
    children: ReactNode;
    requiredRole?: string;
    requiredRoles?: string[];
    redirectTo?: string;
}

export async function PageRoleGuard({ children, requiredRole, requiredRoles, redirectTo = '/unauthorized' }: PageRoleGuardProps)
{
    if (requiredRole)
        await requireRoleWithRedirect(requiredRole, redirectTo);

    else if (requiredRoles && requiredRoles.length > 0)
        await requireAnyRoleWithRedirect(requiredRoles, redirectTo);

    return <>{children}</>;
}

interface ContentRoleGuardProps
{
    children: ReactNode;
    requiredRole?: string;
    requiredRoles?: string[];
    fallback?: ReactNode;
}

export async function ContentRoleGuard({ children, requiredRole, requiredRoles, fallback = null }: ContentRoleGuardProps)
{
    let hasAccess: boolean = false;

    try
    {
        if (requiredRole)
            hasAccess = await requireRole(requiredRole);
    
        else if (requiredRoles && requiredRoles.length > 0)
            hasAccess = await requireAnyRole(requiredRoles);
    }
    catch (error)
    {
        console.error("Error checking role:", error);
        return <>{fallback}</>
    }

    if (!hasAccess)
        return <>{fallback}</>

    return <>{children}</>
}