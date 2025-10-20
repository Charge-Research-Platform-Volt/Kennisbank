import { ReactNode } from 'react';
import { requireRoleWithRedirect, requireAnyRoleWithRedirect, requireRole, requireAnyRole } from '@/lib/auth-server';

interface PageRoleGuardProps
{
    children: ReactNode;
    requiredRole?: string;
    requiredRoles?: string[];
    redirectTo?: string;
}

/**
 * Checks if the page is able to be accessed by the current user
 * @param children - The page
 * @param requiredRole - The role required to access this page
 * @param requiredRoles - Any of these roles are able to access the content
 * @returns The page to be rendered
 */
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

/**
 * Checks if the content is able to be accessed by the current user
 * @param children - The content
 * @param requiredRole - The required role to access this content
 * @param requiredRoles - Any of these roles are able to access the content
 * @param fallback - Fallback content in case the role is insufficient
 * @returns The content to be rendered
 */
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


