"use client"

import LoremIpsum from "@/utils/lorem-ipsum"
import Expandable from "./expandable"
import { Badge } from "@/components/ui/badge"

export function ResourceContent() 
{
    return (
        <>
            <Expandable title="Description" collapsedHeight={100}>
                <LoremIpsum />
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <Badge variant="outline" className="p-2">Tag1</Badge>
                <Badge variant="outline" className="p-2">Tag2</Badge>
                <Badge variant="outline" className="p-2">Tag3</Badge>
                <Badge variant="outline" className="p-2">Tag4</Badge>
                <Badge variant="outline" className="p-2">Tag5</Badge>
                <Badge variant="outline" className="p-2">Tag6</Badge>
                <Badge variant="outline" className="p-2">Tag7</Badge>
                <Badge variant="outline" className="p-2">Tag8</Badge>
                <Badge variant="outline" className="p-2">Tag9</Badge>
                <Badge variant="outline" className="p-2">Tag10</Badge>
                <Badge variant="outline" className="p-2">Tag11</Badge>
                <Badge variant="outline" className="p-2">Tag12</Badge>
                <Badge variant="outline" className="p-2">Tag13</Badge>
                <Badge variant="outline" className="p-2">Tag14</Badge>
                <Badge variant="outline" className="p-2">Tag15</Badge>
                <Badge variant="outline" className="p-2">Tag16</Badge>
                <Badge variant="outline" className="p-2">Tag17</Badge>
                <Badge variant="outline" className="p-2">Tag18</Badge>
                <Badge variant="outline" className="p-2">Tag19</Badge>
                <Badge variant="outline" className="p-2">Tag20</Badge>
            </Expandable>
        </>
    )
}